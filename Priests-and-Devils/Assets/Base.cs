using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mygame;

namespace Mygame
{
    public class SSDirector : System.Object
    {
        private static SSDirector _instance;
        public ISceneController currentSceneController { get; set; }
        public readonly Vector3 from_coast_origin = new Vector3((float)2.5, (float)1.25, 0);
        public readonly Vector3 to_coast_origin = new Vector3((float)9.5, (float)1.25, 0);

        public static SSDirector getInstance()
        {
            if (_instance == null)
            {
                _instance = new SSDirector();
            }
            return _instance;
        }
    }

    //场记接口，实现由FirstController中进行
    public interface ISceneController
    {
        void LoadResources();
    }
    //用户界面与游戏模型的交互接口
    public interface IUserAction
    {
        void restart();
        void moveBoat();
        void clickCharacter(ICharacterController charctrl);
    }

    //移动控制器
    public class MoveController : MonoBehaviour
    {
        float speed = 20f;
        Vector3 destination;
        int status = 0;//0为结束，1为开始

        void Update()
        {
            if (status == 1)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);

                if (Vector3.Distance(transform.position, destination) < 0.0001)
                {
                    status = 0;
                }
            }
        }

        public void Move(Vector3 dest)
        {
            destination = dest;
            status = 1;
        }
    }

    //河岸控制
    public class CoastStorage
    {
        public ICharacterController[] characterStorage;
        public CoastStorage()
        {
            characterStorage = new ICharacterController[6];
        }

        public bool isFull()
        {
            int counter = 0;
            for (int i = 0; i < 6; i++)
            {
                if (characterStorage[i] != null)
                    counter++;
            }
            if (counter == 6)
                return true;
            else
                return false;
        }
        //返回插入的位置
        public int insert(ICharacterController element)
        {
            if (this.isFull())
                return -1;

            for (int i = 0; i < 6; i++)
            {
                if (characterStorage[i] == null)
                {
                    characterStorage[i] = element;
                    return i;
                }
            }
            return -1;
        }

        public ICharacterController getCharacter(int pos)
        {
            if (pos < 0 || pos >= 6)
                return null;
            else
            {
                return characterStorage[pos];
            }
        }
        public bool remove(int pos)
        {
            if (characterStorage[pos] != null)
            {
                characterStorage[pos] = null;
                return true;
            }
            else
                return false;
        }
        public void delete(ICharacterController character)
        {
            for (int i = 0; i < 6; i++)
            {
                if (characterStorage[i] == null)
                    continue;
                else if (characterStorage[i] == character)
                {
                    characterStorage[i] = null;
                    return;
                }
            }
        }
        public void clear()
        {
            characterStorage = new ICharacterController[6];
        }

        public bool check_over(BoatController boat)
        {
            int priest_num = 0, devil_num = 0;
            for (int i = 0; i < characterStorage.Length; i++)
            {
                if (characterStorage[i] == null)
                    continue;
                else if (characterStorage[i].race == "priest")
                {
                    priest_num++;
                }
                else
                {
                    devil_num++;
                }
            }
            if (boat.frontCharacter != null)
            {
                if (boat.frontCharacter.race == "priest")
                {
                    priest_num++;
                }
                else
                {
                    devil_num++;
                }
            }
            if (boat.backCharacter != null)
            {
                if (boat.backCharacter.race == "priest")
                {
                    priest_num++;
                }
                else
                {
                    devil_num++;
                }
            }
            if (devil_num > priest_num && priest_num != 0)
                return true;
            else
                return false;
        }
        public bool check_over()
        {
            int priest_num = 0, devil_num = 0;
            for (int i = 0; i < characterStorage.Length; i++)
            {
                if (characterStorage[i] == null)
                    continue;
                else if (characterStorage[i].race == "priest")
                {
                    priest_num++;
                }
                else
                {
                    devil_num++;
                }
            }
            if (devil_num > priest_num && priest_num != 0)
                return true;
            else
                return false;
        }
    }


    //两个控制器分别控制两岸（包括开始建立，确认是否结束。响应点击事件）
    public class CoastController
    {
        readonly public GameObject coast;
        public CoastStorage storage;

        public CoastController(int status, Vector3 pos)
        {
            coast = Object.Instantiate(Resources.Load("Prefabs/stone", typeof(GameObject))) as GameObject;
            coast.name = "from_coast";
            storage = new CoastStorage();
            if (status == 1)
            {
                coast.transform.position = pos;
                coast.name = "to_coast";
            }
        }

        public void initStorage(ICharacterController[] characters)
        {
            storage.clear();
            for (int i = 0; i < 6; i++)
            {
                storage.insert(characters[i]);
            }
        }

        public int OnCoast(ICharacterController character, int boatStatus)
        {
            if (storage.isFull())
                return -1;
            else
            {
                int pos = storage.insert(character);
                Vector3 relativeVec;
                if (coast.name == "from_coast")
                {
                    relativeVec = new Vector3(2.5f - pos, 1.25f, 0);
                }
                else
                {
                    relativeVec = new Vector3(-2.5f + pos, 1.25f, 0);
                }
                character.moveOffBoat(coast.transform.position, boatStatus, relativeVec);
                return pos;
            }
        }
        public bool OffCoast(int pos)
        {
            bool flag = storage.remove(pos);
            return flag;
        }
        public void OffCoast(ICharacterController Mycharacter)
        {
            storage.delete(Mycharacter);
        }
        public void reset()
        {
            storage.clear();
        }

        public bool check_over(BoatController boat)
        {
            return storage.check_over(boat);
        }
        public bool check_over()
        {
            return storage.check_over();
        }

        public bool check_win()
        {
            return storage.isFull();
        }
    }

    //船的控制器
    public class BoatController
    {
        readonly public GameObject boat;
        readonly MoveController movescript;
        public int boatStatus;//0为从fromCoast开到toCoast，1为回来

        //两个ICharacter对象
        public ICharacterController frontCharacter;
        public ICharacterController backCharacter;

        //两个相对向量，表示相对于船的两个乘员的位置
        readonly Vector3 front = new Vector3(0.5f, 0.5f, 0);
        readonly Vector3 back = new Vector3(-0.5f, 0.5f, 0);
        public BoatController()
        {
            boat = Object.Instantiate(Resources.Load("Prefabs/boat", typeof(GameObject))) as GameObject;
            boat.name = "boat";
            movescript = boat.AddComponent(typeof(MoveController)) as MoveController;
            boat.AddComponent(typeof(UserClick));
            boatStatus = 0;
            frontCharacter = null;
            backCharacter = null;
        }
        public void move()
        {
            if (frontCharacter == null && backCharacter == null)
                return;

            if (boatStatus == 0)
            {
                boatStatus = 1;
                movescript.Move(new Vector3(8, 0, 0));
            }
            else
            {
                boatStatus = 0;
                movescript.Move(new Vector3(4, 0, 0));
            }
        }
        public bool boatFull()
        {
            if (frontCharacter != null && backCharacter != null)
            {
                return true;
            }
            else
                return false;
        }

        //OnBoat和OffBoat，负责操控船的数据结构，同时负责管理移动
        public void OnBoat(ICharacterController element)
        {
            if (this.boatFull())
            {
                return;
            }

            if (boatStatus == 0)//从from向to，front为前
            {
                if (frontCharacter == null)
                {
                    frontCharacter = element;
                    element.character.transform.parent = boat.transform;
                    element.moveOnBoat(boat.transform.position, boatStatus, front);
                }
                else
                {
                    backCharacter = element;
                    element.character.transform.parent = boat.transform;
                    element.moveOnBoat(boat.transform.position, boatStatus, back);
                }
            }
            else // 从to开向from，back为前
            {
                if (backCharacter == null)
                {
                    backCharacter = element;
                    element.character.transform.parent = boat.transform;
                    element.moveOnBoat(boat.transform.position, boatStatus, back);
                }
                else
                {
                    frontCharacter = element;
                    element.character.transform.parent = boat.transform;
                    element.moveOnBoat(boat.transform.position, boatStatus, front);
                }
            }
        }

        //下船需要的支持
        public void OffBoat(ICharacterController element)
        {
            element.character.transform.parent = null;
            if (frontCharacter == element)
            {
                frontCharacter = null;
            }
            else
            {
                backCharacter = null;
            }
        }

        public void reset()
        {
            boatStatus = 0;
            movescript.Move(new Vector3(4, 0, 0));
            frontCharacter = null;
            backCharacter = null;
        }

    }

    //人物控制器
    public class ICharacterController
    {
        readonly public GameObject character;
        readonly public string race;
        readonly UserClick userclick;
        readonly MoveController movescript;
        public bool onBoat;
        public string place;

        readonly Vector3 frontmiddle1 = new Vector3(3.5f, 1.25f, 0);
        readonly Vector3 frontmiddle2 = new Vector3(3.5f, 0.5f, 0);
        readonly Vector3 backmiddle1 = new Vector3(8.5f, 1.25f, 0);
        readonly Vector3 backmiddle2 = new Vector3(8.5f, 0.5f, 0);
        public ICharacterController(int index, string racing, Vector3 pos)
        {
            string path = "Prefabs/" + racing;
            character = Object.Instantiate(Resources.Load(path, typeof(GameObject))) as GameObject;
            character.name = racing + index.ToString();
            character.transform.position = pos;
            race = racing;
            onBoat = false;
            place = "from";

            movescript = character.AddComponent(typeof(MoveController)) as MoveController;

            userclick = character.AddComponent(typeof(UserClick)) as UserClick;
            userclick.setController(this);
        }
        //上船的动作
        public void moveOnBoat(Vector3 pos, int boatStatus, Vector3 relativeMove)
        {
            place = "boat";
            onBoat = true;
            if (boatStatus == 0)
            {
                movescript.Move(frontmiddle1);
                movescript.Move(frontmiddle2);
            }
            else
            {
                movescript.Move(backmiddle1);
                movescript.Move(backmiddle2);
            }
            movescript.Move(pos + relativeMove);


        }
        
        public void moveOffBoat(Vector3 pos, int boatStatus, Vector3 relativeMove)
        {
            onBoat = false;
            if (boatStatus == 0)
            {
                movescript.Move(frontmiddle2);
                movescript.Move(frontmiddle1);
                place = "from";
            }
            else
            {
                movescript.Move(backmiddle2);
                movescript.Move(backmiddle1);
                place = "to";
            }
            movescript.Move(pos + relativeMove);
        }
        public void reset()
        {
            onBoat = false;
            character.transform.parent = null;
            place = "from";
        }
    }
}