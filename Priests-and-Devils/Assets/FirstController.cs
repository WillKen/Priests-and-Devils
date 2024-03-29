﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mygame;

public class FirstController : MonoBehaviour, ISceneController, IUserAction
{
    UserInterface userInterface;

    public CoastController fromCoast;
    public CoastController toCoast;
    public BoatController boat;
    public ICharacterController[] characters;

    void Awake()
    {
        //单例模式的加载
        SSDirector director = SSDirector.getInstance();
        director.currentSceneController = this;
        userInterface = gameObject.AddComponent<UserInterface>() as UserInterface;
        this.LoadResources();
    }

    public void LoadResources()
    {
        GameObject water = Instantiate(Resources.Load("Prefabs/water", typeof(GameObject))) as GameObject;
        fromCoast = new CoastController(0, new Vector3(0, 0, 0));
        toCoast = new CoastController(1, new Vector3(12, 0, 0));
        boat = new BoatController();

        characters = new ICharacterController[6];
        for (int i = 0; i < 3; i++)
        {
            characters[i] = new ICharacterController(i, "priest", new Vector3((float)2.5 - i, (float)1.25, 0));
        }
        for (int i = 3; i < 6; i++)
        {
            characters[i] = new ICharacterController(i, "devil", new Vector3((float)2.5 - i, (float)1.25, 0));
        }
        fromCoast.initStorage(characters);
    }


    public void restart()
    {
        fromCoast.reset();
        toCoast.reset();
        boat.reset();
        for (int i = 0; i < 6; i++)
        {
            characters[i].reset();
            fromCoast.OnCoast(characters[i], 0);
        }

    }
    public void moveBoat()
    {
        if (userInterface.status != 0)
            return;

        boat.move();
        if (boat.boatStatus == 0)//检查船的移动是否造成游戏结束
        {
            if (fromCoast.check_over(boat) || toCoast.check_over())
            {
                userInterface.status = 1;
            }
        }
        else
        {
            if (fromCoast.check_over() || toCoast.check_over(boat))
            {
                userInterface.status = 1;
            }

        }
    }
    public void clickCharacter(ICharacterController charctrl)
    {
        //legal check
        if ((charctrl.place == "from" && boat.boatStatus == 1) || (charctrl.place == "to" && boat.boatStatus == 0))
        {
            return;
        }
        if (userInterface.status != 0)
            return;
        if (boat.boatFull() && charctrl.onBoat == false)
            return;

        CoastController whichCoast;
        if (boat.boatStatus == 0)
        {
            whichCoast = fromCoast;
        }
        else
        {
            whichCoast = toCoast;
        }

        if (charctrl.onBoat == false)   //上船
        {
            whichCoast.OffCoast(charctrl);//离岸
            boat.OnBoat(charctrl);
        }
        else
        {
            whichCoast.OnCoast(charctrl, boat.boatStatus);//下船上岸
            boat.OffBoat(charctrl);
        }

        int flag = checkGameOver();
        Debug.Log("check game over:" + flag);
        if (flag == 1)
        {
            userInterface.status = 2;
        }
        else if (flag == -1)
        {
            userInterface.status = 1;
        }
    }

    //游戏结束失败-1,胜利返回1
    public int checkGameOver()
    {
        Debug.Log("check for game over");

        if (boat.boatStatus == 0)
        {
            if (fromCoast.check_over(boat) || toCoast.check_over())
            {
                return -1;
            }
        }
        else
        {
            Debug.Log("to coast check");
            if (fromCoast.check_over() || toCoast.check_over(boat))
            {
                return -1;
            }
        }

        if (toCoast.check_win())
        {
            return 1;
        }

        return 0;
    }


}
