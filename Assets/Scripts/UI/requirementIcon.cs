using CGTespy.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class requirementIcon : MonoBehaviour
{
    public Sprite closedSprite;
    public Sprite openSprite;
    public string description;
    bool open;
    float closedHeight;
    float openHeight;
    TextMeshProUGUI descriptionText;
    // Start is called before the first frame update
    void Start()
    {
        openHeight = 466.0f;
        closedHeight = 292.0f;
        transform.localScale = new Vector3(0.5f, 0.5f, 0);
        descriptionText = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        setOpen(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void toggleOpen()
    {
        setOpen(!open);
    }

    public void setOpen(bool openStatus)
    {
        // showSchematicGrid check checks the state of the schematic viewer as it should not be interactable
        // Level 1 contains a placeholder object with this name as it does not let you use the schematic feature
        if(openStatus && GameObject.Find("showSchematicGrid"))
        {
            open = true;
            GetComponent<Image>().sprite = openSprite;
            GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, openHeight);
            Vector3 newPos = GetComponent<RectTransform>().localPosition;
            newPos.z = -5;
            GetComponent<RectTransform>().SetLocalPositionAndRotation(newPos, Quaternion.identity);
            descriptionText.text = description;
        }
        else
        {
            open = false;
            GetComponent<Image>().sprite = closedSprite;
            GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, closedHeight);
            Vector3 newPos = GetComponent<RectTransform>().localPosition;
            newPos.z = 0;
            GetComponent<RectTransform>().SetLocalPositionAndRotation(newPos, Quaternion.identity);
            descriptionText.text = "";
        }
    }
}
