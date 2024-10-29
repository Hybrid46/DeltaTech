using System.Collections.Generic;
using UnityEngine;

public class Display : MonoBehaviour
{
    public HoverModule[] hoverModules;
    public Gyroscope gyroModule;
    public Stabilizer stabilizerModule;

    public List<PIDController> controllers = new List<PIDController>();

    void OnGUI()
    {
        //Collect PID controllers
        if (controllers.Count == 0)
        {
            controllers.AddRange(gyroModule.GetControllers());
            controllers.AddRange(stabilizerModule.GetControllers());
        }

        //Hover
        GUILayout.Label("Suspension dampening " + hoverModules[0].suspensionDampening);
        hoverModules[0].suspensionDampening = GUILayout.HorizontalSlider(hoverModules[0].suspensionDampening, 0.0f, 1.0f);
        for (int i = 1; i < hoverModules.Length; i++) hoverModules[i].suspensionDampening = hoverModules[0].suspensionDampening;

        GUILayout.Label("Spring Strength " + hoverModules[0].springStrength);
        hoverModules[0].springStrength = GUILayout.HorizontalSlider(hoverModules[0].springStrength, 50.0f, 500.0f);
        for (int i = 1; i < hoverModules.Length; i++) hoverModules[i].springStrength = hoverModules[0].springStrength;

        GUILayout.Label("Spring Rest Dist " + hoverModules[0].springRestDist);
        hoverModules[0].springRestDist = GUILayout.HorizontalSlider(hoverModules[0].springRestDist, 0.1f, 10.0f);
        for (int i = 1; i < hoverModules.Length; i++) hoverModules[i].springRestDist = hoverModules[0].springRestDist;

        GUILayout.Label("Max Vertical Angle " + hoverModules[0].maxVerticalAngle);
        hoverModules[0].maxVerticalAngle = GUILayout.HorizontalSlider(hoverModules[0].maxVerticalAngle, 5.0f, 50.0f);
        for (int i = 1; i < hoverModules.Length; i++) hoverModules[i].maxVerticalAngle = hoverModules[0].maxVerticalAngle;

        GUILayout.Label("Max Horizontal Angle " + hoverModules[0].maxHorizontalAngle);
        hoverModules[0].maxHorizontalAngle = GUILayout.HorizontalSlider(hoverModules[0].maxHorizontalAngle, 5.0f, 50.0f);
        for (int i = 1; i < hoverModules.Length; i++) hoverModules[i].maxHorizontalAngle = hoverModules[0].maxHorizontalAngle;
        //---------

        //Gyro
        GUILayout.Label("Gyro power " + gyroModule.power);
        gyroModule.power = GUILayout.HorizontalSlider(gyroModule.power, 10.0f, 200.0f);
        //--------

        //PIDs
        GUILayout.Label("PID proportional gain" + controllers[0].proportionalGain);
        controllers[0].proportionalGain = GUILayout.HorizontalSlider(controllers[0].proportionalGain, 0.01f, 1.0f);
        for (int i = 1; i < controllers.Count; i++) controllers[i].proportionalGain = controllers[0].proportionalGain;

        GUILayout.Label("PID integral gain" + controllers[0].integralGain);
        controllers[0].integralGain = GUILayout.HorizontalSlider(controllers[0].integralGain, 0.01f, 1.0f);
        for (int i = 1; i < controllers.Count; i++) controllers[i].integralGain = controllers[0].integralGain;

        GUILayout.Label("PID derivative gain" + controllers[0].derivativeGain);
        controllers[0].derivativeGain = GUILayout.HorizontalSlider(controllers[0].derivativeGain, 0.01f, 1.0f);
        for (int i = 1; i < controllers.Count; i++) controllers[i].derivativeGain = controllers[0].derivativeGain;

        GUILayout.Label("PID integral saturation" + controllers[0].integralSaturation);
        controllers[0].integralSaturation = GUILayout.HorizontalSlider(controllers[0].integralSaturation, 0.01f, 10.0f);
        for (int i = 1; i < controllers.Count; i++) controllers[i].integralSaturation = controllers[0].integralSaturation;
        //--------
    }
}
