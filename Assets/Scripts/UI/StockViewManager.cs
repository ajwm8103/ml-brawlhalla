using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StockViewManager : MonoBehaviour
{

    [Header("StockViews")]
    [SerializeField]
    private StockView[] stockViews;

    [System.Serializable]
    public class StockView
    {
        public LegendAgent agent;
        public GameObject stockViewObject;
        [HideInInspector]
        public Image stockColor;
        [HideInInspector]
        public Image agentIcon;
        [HideInInspector]
        public Text stockNumber;
        [HideInInspector]
        public Text rewardNumber;


        public void Setup()
        {
            stockColor = stockViewObject.transform.Find("StockColor").GetComponent<Image>();
            agentIcon = stockViewObject.transform.Find("AgentIcon").GetComponent<Image>();
            stockNumber = stockViewObject.transform.Find("StockNumber").GetComponent<Text>();
            rewardNumber = stockViewObject.transform.Find("RewardNumber").GetComponent<Text>();
            //Debug.Log(stockColor);
            //Debug.Log(agentIcon);
        }
    }

    private BrawlSettings m_brawlSettings;

    // Start is called before the first frame update
    void Start()
    {
        m_brawlSettings = FindObjectOfType<BrawlSettings>();
        // Set initial sprites
        foreach (StockView stockView in stockViews)
        {
            stockView.Setup();
            foreach (LegendScriptableObject.InputOutputSprite ioSprite in stockView.agent.legend.legendSprites)
            {
                if (ioSprite.team == stockView.agent.team)
                {
                    stockView.agentIcon.sprite = ioSprite.sprite;
                }
            }
            stockView.stockNumber.text = m_brawlSettings.stockCount.ToString();
        }
    }

    public void DisplayStocks(List<BrawlEnvController.AgentInfo> agents)
    {
        foreach (BrawlEnvController.AgentInfo agentInfo in agents)
        {
            foreach (StockView stockView in stockViews)
            {
                if (stockView.agent == agentInfo.agent){
                    float r, g, b;
                    float damage = agentInfo.agent.damage;
                    if (damage < 50){
                        r = 255f;
                        g = 255f;
                        b = 255 - 255 * damage / 50f;
                    } else if (damage < 100){
                        r = 255f;
                        g = 255 - 102 * (damage - 50) / 50f;
                        b = 0f;
                    } else if (damage < 150){
                        r = 255f;
                        g = 153 - 153 * (damage - 100) / 50f;
                        b = 0f;
                    } else if (damage < 200){
                        r = 255 - 64 * (damage - 150) / 50f;
                        g = 0f;
                        b = 0f;
                    } else if (damage < 250){
                        r = 191 - 51 * (damage - 200) / 50f;
                        g = 0f;
                        b = 0f;
                    } else if (damage < 300){
                        r = 140 - 66 * (damage - 250) / 50f;
                        g = 0f;
                        b = 0f;
                    } else {
                        r = 74f;
                        g = 0f;
                        b = 0f;
                    }
                    stockView.stockColor.color = new Color(r / 255f, g / 255f, b / 255f);
                    stockView.stockNumber.text = agentInfo.stocks.ToString();
                    stockView.rewardNumber.text = agentInfo.totalReward.ToString();
                }
            }
        }
    }
}
