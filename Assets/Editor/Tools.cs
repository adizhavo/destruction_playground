using UnityEngine;
using UnityEditor;

namespace GramGames.PirateEvolution.Tools
{
    public class Tools
    {
        [MenuItem("Tools/Clear PlayerPrefs")]
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("All data cleared!");
        }
    }
}