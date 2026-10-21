using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;


public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;

    [Header("Input Actions")]
    [SerializeField] InputActionReference menuAction;       // Menu　(Button)
    [SerializeField] InputActionReference selectAction;     // D-pad (Vector2)
    [SerializeField] InputActionReference decideAction;     // A  (Button)

    void OnEnable()
    {
        Enable(menuAction);
        Enable(selectAction);
        Enable(decideAction);
    }

    static void Enable(InputActionReference r) { if (r != null) r.action.Enable(); }

    // Start is called before the first frame update
    void Start()
    {
        menuPanel.SetActive(false);   
    }

    // Update is called once per frame
    void Update()
    {
        if(Pressed(menuAction))
        {
            Debug.Log("Push Menu Button!");
            ToggleMenu();
        }
    }
    void ToggleMenu() 
    {
        menuPanel.SetActive(!menuPanel.activeSelf);

    }
    static bool Pressed(InputActionReference r)
    {
        return (r != null && r.action.WasPressedThisFrame());
    }

}
