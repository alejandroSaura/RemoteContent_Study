using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponVisual : MonoBehaviour
{
    [SerializeField] string m_testString = "This string is a field that can change in this gameobject's custom script";

    void Start()
    {
        Debug.Log(m_testString);
    }

    private void OnDestroy()
    {
        Debug.Log("Visual destroyed");
    }
}
