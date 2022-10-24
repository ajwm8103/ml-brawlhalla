using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents.Actuators;

public class KeyboardUIManager : MonoBehaviour
{
    private Color inactiveColor;
    private Color activeColor;

    [Header("Keyboards")]
    [SerializeField]
    private KeyboardUIObject[] keyboards;

    private BrawlSettings m_brawlSettings;

    [System.Serializable]
    public class KeyboardUIObject {
        public GameObject keyboard;
        [HideInInspector]
        public Image[] keys;
        public bool active = true;

        public void Setup(){
            keys = new Image[]{
                keyboard.transform.Find("W").GetComponent<Image>(),
                keyboard.transform.Find("S").GetComponent<Image>(),
                keyboard.transform.Find("A").GetComponent<Image>(),
                keyboard.transform.Find("D").GetComponent<Image>(),
                keyboard.transform.Find("Space").GetComponent<Image>(),
                keyboard.transform.Find("H").GetComponent<Image>(),
                keyboard.transform.Find("J").GetComponent<Image>(),
                keyboard.transform.Find("K").GetComponent<Image>(),
                keyboard.transform.Find("L").GetComponent<Image>()
            };
        }
    }
    void Start()
    {
        m_brawlSettings = FindObjectOfType<BrawlSettings>();
        inactiveColor = m_brawlSettings.inactiveColor;
        activeColor = m_brawlSettings.activeColor;
        foreach (KeyboardUIObject keyboard in keyboards)
        {
            if (keyboard.active)
            {
                keyboard.Setup();
            }
        }
    }

    private void FixedUpdate()
    {
        foreach (KeyboardUIObject keyboard in keyboards)
        {
            if (!keyboard.active){
                keyboard.keyboard.SetActive(false);
            }
        }
    }

    public void DisplayAction(Team team, int teamPosition, ActionSegment<int> action)
    {
        KeyboardUIObject keyboard = keyboards[2 * ((int)team) + teamPosition];
        for (int i = 0; i < 9; i++)
        {
            keyboard.keys[i].color = action[i] == 1 ? activeColor : inactiveColor;
        }
    }
}
