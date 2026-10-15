using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ToonBabies
{
    [ExecuteInEditMode]
    [SelectionBase]
    public class TBabyPrefabMaker : MonoBehaviour
    {
        public bool allOptions;
        int hair;
        int body;
        int skintone;
        public bool diaperactive;
        public bool pyjamasactive;

        GameObject GOhead;
        GameObject[] GOhair;
        GameObject[] GObody;
        GameObject GODiaper;
        public Object[] MATSkins;
        public Object[] MATHairA;
        public Object[] MATHairB;
        public Object[] MATHairC;
        public Object[] MATHairD;
        public Object[] MATEyes;
        public Object[] MATDiapers;
        public Object[] MATPyjamas;
        Material headskin;


        void Start()
        {
            allOptions = false;
        }

        public void Getready()
        {
            //load models
            GOhead = transform.Find("HEAD").gameObject as GameObject;
            GODiaper = transform.Find("TB Diaper").gameObject as GameObject;
            GOhair = new GameObject[4];
            GObody = new GameObject[2];

            string[] hairnames = new string[4] { "HairA", "HairB", "HairC", "HairD"};
            string[] bodynames = new string[2] { "TB Body", "TB Pyjamas"};
            for (int forAUX = 0; forAUX < 4; forAUX++) GOhair[forAUX] = transform.Find("ROOT/TB/TB Pelvis/TB Spine/TB Spine1/TB Neck/TB Head/" + hairnames[forAUX]).gameObject as GameObject;
            for (int forAUX = 0; forAUX < 2; forAUX++) GObody[forAUX] = transform.Find(bodynames[forAUX]).gameObject as GameObject;

            if (GObody[0].activeSelf && GObody[1].activeSelf) Randomize();
            else
            {
                for (int forAUX = 0; forAUX < GOhair.Length; forAUX++) { if (GOhair[forAUX].activeSelf) hair = forAUX; }
                while (!GObody[body].activeSelf) body++;
                diaperactive = false;
                if (GODiaper.activeSelf) diaperactive = true;
                pyjamasactive = false;
                if (GObody[1].activeSelf) diaperactive = true;
            }
        }
        void ResetSkin()
        {
            string[] allskins = new string[2] { "ToonBabyA0", "ToonBabyB0"};
            Material[] AUXmaterials;

            int materialcount = GOhead.GetComponent<Renderer>().sharedMaterials.Length;

            //ref head material
            AUXmaterials = GOhead.GetComponent<Renderer>().sharedMaterials;
            materialcount = GOhead.GetComponent<Renderer>().sharedMaterials.Length;
            for (int forAUX2 = 0; forAUX2 < materialcount; forAUX2++)
                for (int forAUX3 = 0; forAUX3 < allskins.Length; forAUX3++)
                    for (int forAUX4 = 1; forAUX4 < 4; forAUX4++)
                    {
                        if (AUXmaterials[forAUX2].name == allskins[forAUX3] + forAUX4)
                        {
                            headskin = AUXmaterials[forAUX2];
                        }
                    }
            //chest
            for (int forAUX = 0; forAUX < GObody.Length; forAUX++)
            {
                AUXmaterials = GObody[forAUX].GetComponent<Renderer>().sharedMaterials;
                materialcount = GObody[forAUX].GetComponent<Renderer>().sharedMaterials.Length;
                for (int forAUX2 = 0; forAUX2 < materialcount; forAUX2++)
                    for (int forAUX3 = 0; forAUX3 < allskins.Length; forAUX3++)
                        for (int forAUX4 = 1; forAUX4 < 6; forAUX4++)
                        {
                            if (AUXmaterials[forAUX2].name == allskins[forAUX3] + forAUX4)
                            {
                                AUXmaterials[forAUX2] = headskin;
                                GObody[forAUX].GetComponent<Renderer>().sharedMaterials = AUXmaterials;
                            }
                        }
            }  
        }
        public void Deactivateall()
        {
            for (int forAUX = 0; forAUX < GOhair.Length; forAUX++) GOhair[forAUX].SetActive(false);
            for (int forAUX = 0; forAUX < GObody.Length; forAUX++) GObody[forAUX].SetActive(false);
            //for (int forAUX = 0; forAUX < GOlegs.Length; forAUX++) GOlegs[forAUX].SetActive(false);
            //for (int forAUX = 0; forAUX < GOfeet.Length; forAUX++) GOfeet[forAUX].SetActive(false);
            GODiaper.SetActive(false);
            diaperactive = false;
        }
        public void Activateall()
        {
            for (int forAUX = 0; forAUX < GOhair.Length; forAUX++) GOhair[forAUX].SetActive(true);
            for (int forAUX = 0; forAUX < GObody.Length; forAUX++) GObody[forAUX].SetActive(true);
            //for (int forAUX = 0; forAUX < GOlegs.Length; forAUX++) GOlegs[forAUX].SetActive(true);
            //for (int forAUX = 0; forAUX < GOfeet.Length; forAUX++) GOfeet[forAUX].SetActive(true);
            GODiaper.SetActive(true);
            diaperactive = true;
        }
        public void Menu()
        {
            allOptions = !allOptions;
        }
        public void Diaperson()
        {
            diaperactive = !diaperactive;
            GODiaper.SetActive(diaperactive);
            if (diaperactive && body == 1) Nextbody();
        }        

        //models
        public void Nexthair()
        {
            if (hair < GOhair.Length) GOhair[hair].SetActive(false);
            hair++;            
            if (hair > GOhair.Length) hair = 0;
            if (hair < GOhair.Length) GOhair[hair].SetActive(true);
        }
        public void Prevhair()
        {
            if (hair < GOhair.Length) GOhair[hair].SetActive(false);
            hair--;            
            if (hair < 0) hair = GOhair.Length;
            if (hair < GOhair.Length) GOhair[hair].SetActive(true);
        }
        public void Nextbody()
        {
            GObody[body].SetActive(false);
            if (body < GObody.Length - 1) body++;
            else body = 0;
            GObody[body].SetActive(true);
            if (body == 0) { diaperactive = false; pyjamasactive = false; }
            else {diaperactive = true; pyjamasactive = true; }
            Diaperson();
        }
        public void Prevbody()
        {
            GObody[body].SetActive(false);
            body--;
            if (body < 0) body = GObody.Length - 1;
            GObody[body].SetActive(true);
            if (body == 0)  pyjamasactive = false; 
            else  pyjamasactive = true; 
        }   

        //materials
        public void Nextskincolor(int todo)
        {
            ChangeMaterials(MATSkins, todo);
        }
        public void Nextdiapers(int todo)
        {
            ChangeMaterials(MATDiapers, todo);
        }
        public void Nexteyescolor(int todo)
        {
            ChangeMaterials(MATEyes, todo);
        }        
        
        public void Nexthaircolor(int todo)
        {
            ChangeMaterials(MATHairA, todo);
            ChangeMaterials(MATHairB, todo);
            ChangeMaterials(MATHairC, todo);
            ChangeMaterials(MATHairD, todo);            
        }
        public void Nextbodycolor(int todo)
        {
            ChangeMaterials(MATPyjamas, todo);
        }       
        
        public void Nude()
        {
            GObody[body].SetActive(false);
            //GOlegs[legs].SetActive(false);
            //GOfeet[feet].SetActive(false);
            body = 0; //legs = 0; feet = 0;
            GObody[0].SetActive(true);
            //GOlegs[0].SetActive(true);
            //GOfeet[0].SetActive(true);
        }
        public void Resetmodel()
        {
            Activateall();
            //ChangeMaterials(MATHat, 3);
            ChangeMaterials(MATSkins, 3);
            ChangeMaterials(MATHairA, 3);
            ChangeMaterials(MATHairB, 3);
            ChangeMaterials(MATHairC, 3);
            ChangeMaterials(MATHairD, 3);
            //ChangeMaterials(MATHairE, 3);
            //ChangeMaterials(MATHairF, 3);
            //ChangeMaterials(MATHairG, 3);
            ChangeMaterials(MATDiapers, 3);
            ChangeMaterials(MATEyes, 3);
            ChangeMaterials(MATPyjamas, 3);
            //ChangeMaterials(MATShirt, 3);
            //ChangeMaterials(MATSweater, 3);
            //ChangeMaterials(MATLegs, 3);
            //ChangeMaterials(MATFeetA, 3);
            //ChangeMaterials(MATFeetB, 3);
            Menu();
        }
        public void Randomize()
        {
            Deactivateall();
            ResetSkin();
            //models
            hair = Random.Range(0, GOhair.Length +1 );
            if (hair < GOhair.Length) GOhair[hair].SetActive(true);
            body = Random.Range(0, GObody.Length);
            GObody[body].SetActive(true);
            if (body == 0)
            {
                diaperactive = true;
                GODiaper.SetActive(true);
                ChangeMaterials(MATDiapers, 2);
                pyjamasactive = false;
            }
            else
            {
                diaperactive = false;
                pyjamasactive = true;
            }

                //materials
                ChangeMaterials(MATEyes, 2);
            for (int forAUX = 0; forAUX < (Random.Range(0, 4)); forAUX++) Nexthaircolor(0);
            for (int forAUX = 0; forAUX < (Random.Range(0, 17)); forAUX++) Nextbodycolor(0);
            for (int forAUX = 0; forAUX < (Random.Range(0, 5)); forAUX++) Nextskincolor(0);

        }
        public void CreateCopy()
        {
            GameObject newcharacter = Instantiate(gameObject, transform.position, transform.rotation);
            for (int forAUX = 23; forAUX > 0; forAUX--)
            {
                if (!newcharacter.transform.GetChild(forAUX).gameObject.activeSelf) DestroyImmediate(newcharacter.transform.GetChild(forAUX).gameObject);
            }
            //if (!GOglasses.activeSelf) DestroyImmediate(newcharacter.transform.Find("ROOT/TK/TK Pelvis/TK Spine/TK Spine1/TK Spine2/TK Neck/TK Head/Glasses").gameObject as GameObject);
            DestroyImmediate(newcharacter.GetComponent<TBabyPrefabMaker>());
        }
        public void FIX()
        {
            GameObject newcharacter = Instantiate(gameObject, transform.position, transform.rotation);
            for (int forAUX = 23; forAUX > 0; forAUX--)
            {
                if (!newcharacter.transform.GetChild(forAUX).gameObject.activeSelf) DestroyImmediate(newcharacter.transform.GetChild(forAUX).gameObject);
            }
            DestroyImmediate(newcharacter.GetComponent<TBabyPrefabMaker>());
            DestroyImmediate(gameObject);
        }


        void ChangeMaterial(GameObject GO, Object[] MAT, int todo)
        {
            bool found = false;
            int MATindex = 0;
            int subMAT = 0;
            Material[] AUXmaterials;
            AUXmaterials = GO.GetComponent<Renderer>().sharedMaterials;
            int materialcount = GO.GetComponent<Renderer>().sharedMaterials.Length;

            for (int forAUX = 0; forAUX < materialcount; forAUX++)
                for (int forAUX2 = 0; forAUX2 < MAT.Length; forAUX2++)
                {
                    if (AUXmaterials[forAUX].name == MAT[forAUX2].name)
                    {
                        subMAT = forAUX;
                        MATindex = forAUX2;
                        found = true;
                    }
                }
            if (found)
            {
                if (todo == 0) //increase
                {
                    MATindex++;
                    if (MATindex > MAT.Length - 1) MATindex = 0;
                }
                if (todo == 1) //decrease
                {
                    MATindex--;
                    if (MATindex < 0) MATindex = MAT.Length - 1;
                }
                if (todo == 2) //random value
                {
                    MATindex = Random.Range(0, MAT.Length);
                }
                if (todo == 3) //reset value
                {
                    MATindex = 0;
                }
                if (todo == 4) //penultimate
                {
                    MATindex = MAT.Length - 2;
                }
                if (todo == 5) //last one
                {
                    MATindex = MAT.Length - 1;
                }
                AUXmaterials[subMAT] = MAT[MATindex] as Material;
                GO.GetComponent<Renderer>().sharedMaterials = AUXmaterials;
            }
        }
        void ChangeMaterials(Object[] MAT, int todo)
        {
            for (int forAUX = 0; forAUX < GOhair.Length; forAUX++) ChangeMaterial(GOhair[forAUX], MAT, todo);
            ChangeMaterial(GOhead, MAT, todo);
            ChangeMaterial(GODiaper, MAT, todo);
            //ChangeMaterial(GOheadsimple, MAT, todo);
            for (int forAUX = 0; forAUX < GObody.Length; forAUX++) ChangeMaterial(GObody[forAUX], MAT, todo);
            //for (int forAUX = 0; forAUX < GOlegs.Length; forAUX++) ChangeMaterial(GOlegs[forAUX], MAT, todo);
            //for (int forAUX = 0; forAUX < GOfeet.Length; forAUX++) ChangeMaterial(GOfeet[forAUX], MAT, todo);
        }
        void SwitchMaterial(GameObject GO, Object[] MAT1, Object[] MAT2)
        {
            Material[] AUXmaterials;
            AUXmaterials = GO.GetComponent<Renderer>().sharedMaterials;
            int materialcount = GO.GetComponent<Renderer>().sharedMaterials.Length;
            int index = 0;
            for (int forAUX = 0; forAUX < materialcount; forAUX++)
                for (int forAUX2 = 0; forAUX2 < MAT1.Length; forAUX2++)
                {
                    if (AUXmaterials[forAUX].name == MAT1[forAUX2].name)
                    {
                        index = forAUX2;
                        if (forAUX2 > MAT2.Length - 1) index -= (int)Mathf.Floor(index / 4) * 4;
                        AUXmaterials[forAUX] = MAT2[index] as Material;
                        GO.GetComponent<Renderer>().sharedMaterials = AUXmaterials;
                    }
                }
        }

        public void Test()
        { }
    }
}