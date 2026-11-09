using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class GuardType_Add : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown guardType;

    // Start is called before the first frame update
    void Start()
    {
        guardType = GetComponent<TMP_Dropdown>();

        List<string> options = new List<string> { "Easy", "Normal", "hard" };
        guardType.AddOptions(options);
        guardType.RefreshShownValue();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
