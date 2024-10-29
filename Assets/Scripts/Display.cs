using UnityEngine;

public class Display : MonoBehaviour
{
    public HoverModule[] hoverModules;

    void OnGUI()
    {
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
    }
}
