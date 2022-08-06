using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/SavedData")]
public class SavedData : ScriptableObject
{
    public int PreferredResolutionIndex = 2;
    public int PreferredFullScreenMode = 1;
    
}