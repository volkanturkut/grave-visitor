using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GravePointTests
{
    [SetUp]
    public void SetUp()
    {
        // Ensure clean state before each test
        GravePoint.AllGraves.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        // Cleanup static list to avoid side effects
        GravePoint.AllGraves.Clear();
    }

    [UnityTest]
    public IEnumerator GravePoint_OnEnable_AddsToAllGraves()
    {
        // Arrange
        var go = new GameObject("GravePointTest");
        go.SetActive(false); // Ensure OnEnable isn't called yet
        var gravePoint = go.AddComponent<GravePoint>();

        // Act
        go.SetActive(true);
        yield return null; // Wait for frame

        // Assert
        Assert.Contains(gravePoint, GravePoint.AllGraves, "GravePoint should be added to AllGraves when enabled.");

        // Cleanup
        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator GravePoint_OnDisable_RemovesFromAllGraves()
    {
        // Arrange
        var go = new GameObject("GravePointTest");
        var gravePoint = go.AddComponent<GravePoint>();
        yield return null; // Wait for OnEnable

        // Pre-Assert
        Assert.Contains(gravePoint, GravePoint.AllGraves, "GravePoint should be in AllGraves initially.");

        // Act
        go.SetActive(false);
        yield return null; // Wait for frame

        // Assert
        Assert.IsFalse(GravePoint.AllGraves.Contains(gravePoint), "GravePoint should be removed from AllGraves when disabled.");

        // Cleanup
        Object.Destroy(go);
    }
}
