using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] int score=0;
    [SerializeField] public float bonusRecargaActivo=1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BonusRecarga(int opcion)
    {
        switch (opcion)
        {
            case 0:
                //Aumenta boonus
                if (bonusRecargaActivo < 1.3f)
                {
                    bonusRecargaActivo += 0.1f;
                }
                break;
            case 1:
                //Disminuye bonus
                if (bonusRecargaActivo > 1)
                {
                    bonusRecargaActivo -= 0.1f;
                }                
                break;
        }
    }

}
