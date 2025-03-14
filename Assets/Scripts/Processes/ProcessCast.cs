using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProcessCast : ProcessParent
{


    public override bool CallStep(int i)
    {
        bool toReturn = false;
        /*if(layerStackHold.curMaterial == control.materialType.gold)
        {
            toReturn = layerStackHold.depositGoldLayer(control.materialType.cast, BitGrid.ones(), i + 1);
            if (!toReturn)
            {
                ErrorMessage = "No chromium or gold to deposit on!";
            }
        }
        else*/
        {
            toReturn = layerStackHold.depositWetLayer(control.materialType.cast, BitGrid.ones(), i + 1);
            if (!toReturn)
            {
                ErrorMessage = "No room to deposit material!";
            }
        }
        return toReturn;
    }

    public override void OnValueChanged(float newValue)
    {
        curStep = (int) newValue;
        layerStackHold.sliceDeposits(curStep);
    }

    public override void UpdateSchematics() {

        schematicManager schematicManagerObject = GameObject.Find("schematicManager").GetComponent<schematicManager>();

        if (schematicManagerObject)
        {
            schematicManagerObject.updateText("Cast");
            schematicManagerObject.updateSchem = true;
        }
    }

}
