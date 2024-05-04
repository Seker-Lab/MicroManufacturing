using UnityEngine;

public class ConversationText : TextParent{
    [SerializeField] private string question;
    [SerializeField] private string[] answers;
    [SerializeField] private int key;
    [SerializeField] private Vector2 location;
    [SerializeField] private TextAnchor anchor;
    
    /*
     * TODO: Add buttons that will be nuked when conversation text is hidden.
     */
    private int charPerLine = 50;
    private int charCount = 0;
    override public void Initialize()
    {
        base.Initialize();

        string textWithLines = addWord(question);
        
        foreach (string answer in answers)
        {
            textWithLines += "\n";
            textWithLines += "<color=#adb9c4>\t•" + addWord(answer) + "</color>";
        }

        string newText = "";
        bool firstStar = true;
        foreach (char c in textWithLines){
            string curChar = c.ToString();
            if(curChar == "*")
            {
                if (firstStar)
                {
                    curChar = "<color=#03B4FF><i>";
                    firstStar = false;
                }
                else
                {
                    curChar = "</color></i>";
                    firstStar = true;
                }
            }
            newText += curChar;
        }
        question = newText;
    }

    private string addWord(string word)
    {
        string textWithLines = "";
        foreach (char c in word)
        {
            if (c == '■')
            {
                charCount = -1;
                textWithLines += "\n";
                continue;
            }
            textWithLines += c;
            if (c == '*')
            {
                continue;
            }
            if (charCount >= charPerLine)
            {
                if (c == ' ')
                {
                    charCount = -1;
                    textWithLines += "\n";

                }
            }
            charCount++;
        }

        return textWithLines;
    }
    override public void Display()
    {
        base.Display();
        textBoxText.text = question;
        RectTransform dialogueSystem = GameObject.Find("DialogueSystem").GetComponent<RectTransform>();
        CGTespy.UI.RectTransformPresetApplyUtils.ApplyAnchorPreset(textBoxPosition.GetComponent<RectTransform>(), anchor);
        CGTespy.UI.RectTransformPresetApplyUtils.ApplyAnchorPreset(dialogueSystem, anchor);
        dialogueSystem.anchoredPosition = new Vector3(0, 0, 0);
        textBoxPosition.GetComponent<RectTransform>().anchoredPosition = location;
        //control.setPaused(control.pauseStates.tutorialPaused);
    }
}