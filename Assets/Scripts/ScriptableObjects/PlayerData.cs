using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "BallonPrototype/PlayerData", order = 1)]
public class PlayerData : ScriptableObject
{
    [Tooltip("Normal Walk Speed")]
    public float WalkSpeed = 2f;
    [Tooltip("Mouse Sensitivity")]
    public float CameraRotationSpeed = 3f;
    [Tooltip("Normal Jump Speed")]
    public float JumpInitialSpeed = 5f;
    public float GroundCastLength = 1.1f;
    public LayerMask GroundLayer;
    public float MaxEnergy = 100f;
    public float OnGroundEnergyRecover = -50f;
    [Tooltip("How Much per second Booste Speed Increment when Holding Space")]
    public float BoosterSpeedIncrementalSpeed = 10f;
    [Tooltip("How Much per second Holding Space Spend Energy")]
    public float BoosterSpeedEnergyUseage = 5f;
    public float MaxBoosterSpeed = 20f;
    [Tooltip("Force Holding Glide Up, 0 makes it free fall")]
    public float GlideUpForce = 9f;
    [Tooltip("How much energy gliding uses per second")]
    public float GlideEnergyUsage = -1f;
    public float GlideSpeed = 10f;
    public float GlideTurnSpeed = 1f;
    public float MaxGlideZRotation = 30f;
    public float GlideZRotationRecoverSpeed = 30f;
    public float EnableGlideModeBoosterForceThreshold = 3f;
}
