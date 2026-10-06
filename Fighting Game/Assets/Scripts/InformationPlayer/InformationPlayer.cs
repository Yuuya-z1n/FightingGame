using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InformationPlayer : MonoBehaviour
{

    enum PlayerState
    {
        Low,
        Middle,
        High
    }


    public GameObject Player;
    [SerializeField] private GameObject InfomationPlayer;
    int[] LightPanchDamage = { 200, 300, 400 };
    int[] MiddlePanchDamage = {500, 600,700};
    int[] HighPanchDamage = {700 ,800,900};
    int[] LightKickDamage = { 200, 300, 400 };
    int[] MiddleKickDamage = {600, 700,800};
    int[] HighKickDamage = {800, 900,1000};

   

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
