using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillBagPanel : BasePanel
{
    SkillSender skillSender;
    GameObject newSlot;
    Button bagButton;
    static readonly string path = "Prefab/UI/SkillBag";
    public SkillBagPanel(SkillSender sender,GameObject slot) : base(new UItype(path))
    {
        skillSender = sender;
        newSlot = slot;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        GameObject bag = uItool.FindChildGameobj("bagExit");
        if (bag!=null)
        {
            bagButton = bag.GetComponent<Button>();
            bagButton.onClick.RemoveAllListeners();
            bagButton.onClick.AddListener(() =>
            {
               panelManager.Pop();
            });
        }
        ScanBag();
    }


    void ScanBag()
    {
        // Debug.Log($"skillbagCount: {skillSender.skillBag.Count}");
        GameObject slot = uItool.FindChildGameobj("Scroll View/Viewport/bagslot");
        if (!slot) return;
        uItool.RemoveAllChildren(slot);
        foreach(var item in skillSender.skillBag)
        {
            GameObject child = GameObject.Instantiate(newSlot,slot.transform);
            Button button = child.GetComponent<Button>();
            Image image = child.GetComponent<Image>();
            Text text = child.GetComponentInChildren<Text>();

            image.sprite = item.icon;
            text.text = item.Description;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if(skillSender.EquipSkill(item))
                
                GameObject.Destroy(child);
            });

            // 从 newSlot 预制体收集冷却 UI 元素（仿 BuffSlotManager，null 安全）
            Image bg = uItool.FindChildGameobj(child, "background")?.GetComponent<Image>();
            bg.sprite = item.icon;
            TextMeshProUGUI cdTextMesh = child.GetComponentInChildren<TextMeshProUGUI>();
            cdTextMesh.text = "";

        }
    }

}
