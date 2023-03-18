using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LanguageSwitcher : MonoBehaviour
{
    //UCSB Stands for "Unity Chan Spring Bone";
    //JLM Stands for "Japanese Language Mode";

    [MenuItem("UCSB/Set Language To English")]
    public static void SwitchToEnglish()
    {
        EditorPrefs.SetBool("UCSB_JLM", false);
    }

    [MenuItem("UCSB/Set Language To Japanese")]
    public static void SwitchToJapanese()
    {
        EditorPrefs.SetBool("UCSB_JLM", true);
    }
}
