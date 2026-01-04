using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
using System.Collections.Generic; // Stack için gerekli

public class StoneCarver : MonoBehaviour
{
    [Header("References")]
    public StarterAssetsInputs input;
    public RawImage carvableLayer;
    public RectTransform cursorVisual;

    [Header("Settings")]
    public float brushSize = 0.05f;
    public float gamepadCursorSpeed = 800f;
    public int maxUndoSteps = 10; // Hafıza şişmesin diye sınır koyuyoruz

    [Header("Effects")]
    public ParticleSystem dustEffects;
    public AudioSource carveAudio;

    private Texture2D _textureInstance;
    private Vector2 _virtualCursorPos;

    // UNDO İÇİN GEREKLİ DEĞİŞKENLER
    private Stack<Color[]> _undoStack = new Stack<Color[]>();
    private bool _isCarving = false; // Tuşa basılı tutup tutmadığımızı takip eder

    void Start()
    {
        if (carvableLayer.texture == null) return;

        Texture2D original = (Texture2D)carvableLayer.texture;
        _textureInstance = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
        _textureInstance.SetPixels(original.GetPixels());
        _textureInstance.Apply();

        carvableLayer.texture = _textureInstance;
        _virtualCursorPos = new Vector2(Screen.width / 2, Screen.height / 2);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }

    void Update()
    {
        UpdateCursorPosition();
        HandleCarving();
        HandleUndo(); // Undo kontrolünü buraya ekledik
    }

    void HandleUndo()
    {
        // Eğer Undo tuşuna basıldıysa VE hafızada geri alınacak bir şey varsa
        if (input.undo && _undoStack.Count > 0)
        {
            PerformUndo();
            input.undo = false; // Sürekli geri almasın diye flag'i indiriyoruz
        }
    }

    void PerformUndo()
    {
        // 1. Stack'in en üstündeki (en son kaydedilen) pixel dizisini al
        Color[] previousPixels = _undoStack.Pop();

        // 2. Texture'a uygula
        _textureInstance.SetPixels(previousPixels);
        _textureInstance.Apply();

        Debug.Log("Geri alındı. Kalan adım hakkı: " + _undoStack.Count);
    }

    void SaveUndoState()
    {
        // Hafıza dolduysa en eski kaydı sil (Performans için)
        if (_undoStack.Count >= maxUndoSteps)
        {
            // Stack yapısında en alttakini silmek zordur, 
            // basitçe sonuncuyu atıp yeni listeye çevirebiliriz ama
            // bu örnekte basit tutmak için sadece ekliyoruz.
            // (Çok gelişmiş sistemlerde Deque kullanılır)
            // Stack dolunca en eskiyi silmek yerine en yeniyi eklemeyiz veya listeyi ters çeviririz.
            // Unity Garbage Collector'ı yormamak için basit çözüm:
            // 10 adımı geçerse en alttakini siliyoruz (List'e çevirip).

            var list = new List<Color[]>(_undoStack);
            list.RemoveAt(list.Count - 1); // En eskiyi sil
            _undoStack = new Stack<Color[]>(list);
            // Not: Bu işlem biraz maliyetlidir ama 10 adımda sorun olmaz.
        }

        // Şu anki piksellerin bir kopyasını al ve sakla
        _undoStack.Push(_textureInstance.GetPixels());
    }

    void HandleCarving()
    {
        // 1. START CARVING (Pressed button just now)
        if (input.carve && !_isCarving)
        {
            SaveUndoState();
            _isCarving = true;

            if (carveAudio && !carveAudio.isPlaying) carveAudio.Play();

            // Start Particles
            if (dustEffects)
            {
                dustEffects.Play();
                var emission = dustEffects.emission;
                emission.enabled = true;
            }
        }
        // 2. WHILE CARVING (Holding button)
        else if (input.carve && _isCarving)
        {
            CarveAt(_virtualCursorPos);
            // No need to update particle position if it is a child of CursorVisual!
        }
        // 3. STOP CARVING (Released button)
        else if (!input.carve && _isCarving)
        {
            _isCarving = false;

            if (carveAudio) carveAudio.Stop();

            // Stop Particles
            if (dustEffects)
            {
                // We use Stop() so existing particles finish falling naturally
                dustEffects.Stop();
            }
        }
    }

    // ... UpdateCursorPosition, CarveAt ve PunchHole fonksiyonların aynen kalacak ...
    void UpdateCursorPosition()
    {
        // 1. Sanal İmleç Verisini Hesapla (Burası aynı kalıyor - Ekran verisi)
        if (input.moveBrush.magnitude > 0.1f)
        {
            _virtualCursorPos += input.moveBrush * gamepadCursorSpeed * Time.deltaTime;
        }
        else if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.delta.ReadValue().magnitude > 0)
        {
            _virtualCursorPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }

        // Ekran sınırlarında tut
        _virtualCursorPos.x = Mathf.Clamp(_virtualCursorPos.x, 0, Screen.width);
        _virtualCursorPos.y = Mathf.Clamp(_virtualCursorPos.y, 0, Screen.height);

        // --- DEĞİŞEN KISIM BURASI ---

        if (cursorVisual != null)
        {
            // Canvas "Screen Space - Camera" modunda olduğu için,
            // Ekran Pozisyonunu (2D), Dünya Pozisyonuna (3D) çevirmemiz lazım.

            Vector3 worldPoint;

            // Bu Unity fonksiyonu, kamerayı referans alarak doğru 3D noktayı bulur
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)cursorVisual.parent, // İmlecin ebeveynini referans alıyoruz
                _virtualCursorPos,
                Camera.main, // Sahnedeki Main Camera'yı kullanıyoruz
                out worldPoint
            );

            cursorVisual.position = worldPoint;

            // İmleci Z ekseninde biraz öne çekelim ki taşın içine girmesin
            // (Canvas Camera modunda Z derinliği önemlidir)
            Vector3 visualPos = cursorVisual.localPosition;
            visualPos.z = -1; // -1 diyerek kameraya biraz yaklaştırıyoruz
            cursorVisual.localPosition = visualPos;
        }
    }

    void CarveAt(Vector2 screenPos)
    {
        RectTransform rectTransform = carvableLayer.rectTransform;
        Vector2 localPoint;

        // DÜZELTME BURADA:
        // 'null' yerine 'Camera.main' yazıyoruz.
        // Çünkü Canvas artık Camera modunda, hesaplama için kameraya ihtiyacı var.

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            screenPos,
            Camera.main, // <-- BURAYI DEĞİŞTİR
            out localPoint))
        {
            // Normalize (0 to 1)
            float uvX = (localPoint.x + rectTransform.rect.width / 2) / rectTransform.rect.width;
            float uvY = (localPoint.y + rectTransform.rect.height / 2) / rectTransform.rect.height;

            // Map to Pixels
            int x = (int)(uvX * _textureInstance.width);
            int y = (int)(uvY * _textureInstance.height);

            PunchHole(x, y);
        }
    }

    void PunchHole(int centerX, int centerY)
    {
        int r = (int)(_textureInstance.width * brushSize);
        for (int i = -r; i <= r; i++)
        {
            for (int j = -r; j <= r; j++)
            {
                if (i * i + j * j <= r * r)
                {
                    int px = centerX + i;
                    int py = centerY + j;
                    if (px >= 0 && px < _textureInstance.width && py >= 0 && py < _textureInstance.height)
                    {
                        Color pixelColor = _textureInstance.GetPixel(px, py);
                        pixelColor.a = 0;
                        _textureInstance.SetPixel(px, py, pixelColor);
                    }
                }
            }
        }
        _textureInstance.Apply();
    }
}