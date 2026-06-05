using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerAnimationTests
{
    [SetUp]
    public void SetUp() => TestUtils.CleanupStatics();

    [TearDown]
    public void TearDown() => TestUtils.CleanupStatics();

    [UnityTest]
    public IEnumerator Player_HasAnimatorAndController_WhenGameStarts()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var player = GameObject.Find("Player");
        Assert.IsNotNull(player, "Player GameObject not found");

        var animator = player.GetComponent<Animator>();
        Assert.IsNotNull(animator, "Player has no Animator component");
        Assert.IsNotNull(animator.runtimeAnimatorController,
            "Player Animator has no RuntimeAnimatorController assigned");
    }

    [UnityTest]
    public IEnumerator Player_IdleStateOnStart_WhenNotMoving()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var player = GameObject.Find("Player");
        var animator = player.GetComponent<Animator>();

        // Wait a frame for animator to initialize
        yield return null;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        Assert.IsTrue(stateInfo.IsName("idle"),
            $"Expected idle state but got current state with hash {stateInfo.shortNameHash}");
    }

    [UnityTest]
    public IEnumerator Player_SwitchesToRunState_WhenSpeedParameterSet()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var player = GameObject.Find("Player");
        var animator = player.GetComponent<Animator>();

        // Simulate running by setting Speed parameter
        animator.SetFloat("Speed", 1f);

        // Wait for transition to complete
        yield return TestUtils.WaitForState(
            () => animator.GetCurrentAnimatorStateInfo(0).IsName("frontRun"),
            2f,
            "Animator did not transition to frontRun state within 2 seconds");

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        Assert.IsTrue(stateInfo.IsName("frontRun"),
            "Expected frontRun state after setting Speed=1");
    }

    [UnityTest]
    public IEnumerator Player_ReturnsToIdle_WhenSpeedBackToZero()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var player = GameObject.Find("Player");
        var animator = player.GetComponent<Animator>();

        // Go to run state first
        animator.SetFloat("Speed", 1f);
        yield return TestUtils.WaitForState(
            () => animator.GetCurrentAnimatorStateInfo(0).IsName("frontRun"),
            2f, "Did not reach frontRun state");

        // Stop moving
        animator.SetFloat("Speed", 0f);

        // Wait for transition back to idle
        yield return TestUtils.WaitForState(
            () => animator.GetCurrentAnimatorStateInfo(0).IsName("idle"),
            2f,
            "Animator did not transition back to idle state within 2 seconds");

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        Assert.IsTrue(stateInfo.IsName("idle"),
            "Expected idle state after setting Speed=0");
    }

    [UnityTest]
    public IEnumerator Player_AnimationClipsExist_AndHaveFrames()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var player = GameObject.Find("Player");
        var animator = player.GetComponent<Animator>();
        var controller = animator.runtimeAnimatorController;

        Assert.IsNotNull(controller, "AnimatorController is null");
        Assert.GreaterOrEqual(controller.animationClips.Length, 2,
            $"Expected at least 2 animation clips, got {controller.animationClips.Length}");

        foreach (var clip in controller.animationClips)
        {
            Assert.IsNotNull(clip, $"AnimationClip is null in controller");
            Assert.Greater(clip.length, 0f,
                $"AnimationClip '{clip.name}' has zero length");
        }
    }
}
