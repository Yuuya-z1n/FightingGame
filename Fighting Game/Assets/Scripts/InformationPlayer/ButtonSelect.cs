using UnityEngine;
using UnityEngine.UI;

public class ButtonSelect : MonoBehaviour
{
    public GameObject SelectmyParent;
    public Button[] buttons;


    private int selectedIndex = 1;

    void Start()
    {

    }

    public void Select(int index)
    {
        selectedIndex = index;

    }
    void Update()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i == selectedIndex)
            {
                // ‘I‘ð’†
                buttons[i].image.color = Color.red;
            }
            else
            {
                // ‘I‘ð‚³‚ê‚Ä‚¢‚È‚¢
                buttons[i].image.color = Color.white;
            }
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            ChangeButton(-1);
            Debug.Log("A‚ð‰Ÿ‚µ‚Ä‚¢‚é");

        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            ChangeButton(1);
            Debug.Log("D‚ð‰Ÿ‚µ‚Ä‚¢‚é");
        }
    }

    void ChangeButton(int dir)
    {
        selectedIndex += dir;

        if (selectedIndex >= buttons.Length)
        {
            selectedIndex = buttons.Length;
        }

        if (selectedIndex <= 0)
        {
            selectedIndex = 0;
        }
    }
}