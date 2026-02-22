using NUnit.Framework;
using UnityEngine;
using System.Reflection;

public class VisitorAITests
{
    private GameObject _visitorGO;
    private VisitorAI _visitor;
    private GameObject _timeControllerGO;
    private DayNightController _timeController;

    [SetUp]
    public void Setup()
    {
        _visitorGO = new GameObject("Visitor");
        // VisitorAI requires NavMeshAgent and Animator, which are automatically added.
        _visitor = _visitorGO.AddComponent<VisitorAI>();

        _timeControllerGO = new GameObject("TimeController");
        _timeController = _timeControllerGO.AddComponent<DayNightController>();
    }

    [TearDown]
    public void Teardown()
    {
        if (_visitorGO != null)
            Object.DestroyImmediate(_visitorGO);

        if (_timeControllerGO != null)
            Object.DestroyImmediate(_timeControllerGO);
    }

    private bool InvokeIsVisitingHours()
    {
        MethodInfo method = typeof(VisitorAI).GetMethod("IsVisitingHours", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "Could not find private method 'IsVisitingHours' in VisitorAI");
        return (bool)method.Invoke(_visitor, null);
    }

    [Test]
    public void IsVisitingHours_DayShift_WithinHours_ReturnsTrue()
    {
        // Arrange
        // Open: 08:00, Close: 17:00
        _visitor.Initialize(_timeController, Vector3.zero, 8f, 17f);
        _timeController.currentTime = 12f; // Noon

        // Act
        bool result = InvokeIsVisitingHours();

        // Assert
        Assert.IsTrue(result, "Expected to be visiting hours at 12:00 for 08:00-17:00 shift");
    }

    [Test]
    public void IsVisitingHours_DayShift_BeforeOpen_ReturnsFalse()
    {
        // Arrange
        // Open: 08:00, Close: 17:00
        _visitor.Initialize(_timeController, Vector3.zero, 8f, 17f);
        _timeController.currentTime = 7f; // 07:00

        // Act
        bool result = InvokeIsVisitingHours();

        // Assert
        Assert.IsFalse(result, "Expected NOT to be visiting hours at 07:00 for 08:00-17:00 shift");
    }

    [Test]
    public void IsVisitingHours_DayShift_AfterClose_ReturnsFalse()
    {
        // Arrange
        // Open: 08:00, Close: 17:00
        _visitor.Initialize(_timeController, Vector3.zero, 8f, 17f);
        _timeController.currentTime = 18f; // 18:00

        // Act
        bool result = InvokeIsVisitingHours();

        // Assert
        Assert.IsFalse(result, "Expected NOT to be visiting hours at 18:00 for 08:00-17:00 shift");
    }

    [Test]
    public void IsVisitingHours_NightShift_WithinHours_BeforeMidnight_ReturnsTrue()
    {
        // Arrange
        // Open: 22:00, Close: 05:00 (Night shift)
        _visitor.Initialize(_timeController, Vector3.zero, 22f, 5f);
        _timeController.currentTime = 23f; // 23:00

        // Act
        bool result = InvokeIsVisitingHours();

        // Assert
        Assert.IsTrue(result, "Expected to be visiting hours at 23:00 for 22:00-05:00 shift");
    }

    [Test]
    public void IsVisitingHours_NightShift_WithinHours_AfterMidnight_ReturnsTrue()
    {
        // Arrange
        // Open: 22:00, Close: 05:00 (Night shift)
        _visitor.Initialize(_timeController, Vector3.zero, 22f, 5f);
        _timeController.currentTime = 2f; // 02:00

        // Act
        bool result = InvokeIsVisitingHours();

        // Assert
        Assert.IsTrue(result, "Expected to be visiting hours at 02:00 for 22:00-05:00 shift");
    }

    [Test]
    public void IsVisitingHours_NightShift_OutsideHours_ReturnsFalse()
    {
        // Arrange
        // Open: 22:00, Close: 05:00 (Night shift)
        _visitor.Initialize(_timeController, Vector3.zero, 22f, 5f);
        _timeController.currentTime = 12f; // Noon

        // Act
        bool result = InvokeIsVisitingHours();

        // Assert
        Assert.IsFalse(result, "Expected NOT to be visiting hours at 12:00 for 22:00-05:00 shift");
    }
}
