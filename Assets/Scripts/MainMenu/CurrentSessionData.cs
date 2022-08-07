using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/CurrentSessionData")]
public class CurrentSessionData : ScriptableObject
{
    // Make sure this has the default value when building the application. It might have a different one after playing in the Unity editor
    public int GameTipIndex = -1;
}