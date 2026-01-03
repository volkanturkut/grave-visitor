using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Ghost/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string speakerName;
    [TextArea(3, 10)]
    public string[] sentences;
}