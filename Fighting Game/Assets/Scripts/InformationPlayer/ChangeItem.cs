using UnityEngine;
using UnityEngine.UI;

public class ChangeItems : MonoBehaviour
{
    public GameObject[] ChangeItem;
    public int selectedIndex = 0;

    void Start()
    {

    }

    public void Select(int index)
    {
        selectedIndex = index;

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            ChangeButton(-1);
            Debug.Log("S‚ð‰Ÿ‚µ‚Ä‚¢‚é");

        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            ChangeButton(1);
            Debug.Log("W‚ð‰Ÿ‚µ‚Ä‚¢‚é");
        }
    }

    void ChangeButton(int dir)
    {
        selectedIndex += dir;

        if (selectedIndex >= ChangeItem.Length)
        {
            selectedIndex = ChangeItem.Length;
        }

        if (selectedIndex <= 0)
        {
            selectedIndex = 0;
        }
    }
}