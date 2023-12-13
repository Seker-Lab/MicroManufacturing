using UnityEngine;

public class ProcessCast : ProcessParent
{
    public override bool CallStep(int i)
    {
        bool toReturn = layerStackHold.depositLayer(layerStackHold.curMaterial, BitGrid.ones(), i + 1);
        if (!toReturn)
        {
            ErrorMessage = "No room to deposit material!";
        }
        return toReturn;
        
    }

    public override void OnValueChanged(float newValue)
    {
        curStep = (int)newValue;
        layerStackHold.sliceDeposits(curStep);
    }

    public override void UpdateSchematics()
    {

        schematicManager schematicManagerObject = GameObject.Find("schematicManager").GetComponent<schematicManager>();

        if (schematicManagerObject)
        {
            schematicManagerObject.updateText("Cast");
            schematicManagerObject.updateSchem = true;
        }
    }
}
