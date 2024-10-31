using AbilityApi;
using BepInEx;
using BoplFixedMath;
using HarmonyLib;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using static System.Net.Mime.MediaTypeNames;

namespace LongAbility
{
    [BepInPlugin("org.000diggity000.LongAbility", "LongAbility", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static System.IO.Stream GetResourceStream(string namespaceName, string path) => Assembly.GetExecutingAssembly().GetManifestResourceStream($"{namespaceName}.{path}");
        private void Awake()
        {
            Harmony harmony = new Harmony("org.000diggity000.LongAbility");
            harmony.PatchAll();
            var directoryToModFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var LongAbilityPrefab = ConstructAbility<ExplodeAbility>("LongAbility");
            LongAbilityPrefab.gameObject.AddComponent<PlayerPhysics>();
            Texture2D LongAbilityTex = new Texture2D(1, 1);
            LongAbilityTex.LoadImage(ReadFully(GetResourceStream("LongAbility", "AbilityIcon.png")));
            var iconSprite = Sprite.Create(LongAbilityTex, new Rect(0f, 0f, LongAbilityTex.width, LongAbilityTex.height), new Vector2(0.5f, 0.5f));
            NamedSprite longAbility = new NamedSprite("Long Ability", iconSprite, LongAbilityPrefab.gameObject, true);
            Api.RegisterNamedSprites(longAbility, true);
        }
        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
        public static T ConstructAbility<T>(string name) where T : MonoUpdatable
        {
            GameObject parent = new GameObject(name);
            GameObject.DontDestroyOnLoad(parent);

            Ability ability = parent.AddComponent<Ability>();

            parent.AddComponent<FixTransform>();
            parent.AddComponent<SpriteRenderer>();
            Texture2D LongAbilityTex = new Texture2D(2, 2);
            LongAbilityTex.LoadImage(ReadFully(GetResourceStream("LongAbility", "gun.png")));
            var iconSprite = Sprite.Create(LongAbilityTex, new Rect(0f, 0f, LongAbilityTex.width, LongAbilityTex.height), new Vector2(0.5f, 0.5f));
            parent.GetComponent<SpriteRenderer>().sprite = iconSprite;
            parent.AddComponent<PlayerBody>();
            parent.AddComponent<DPhysicsBox>();
            parent.AddComponent<PlayerCollision>();
            MonoUpdatable updatable = parent.AddComponent<T>();

            if (updatable == null)
            {
                GameObject.Destroy(parent);
                throw new MissingReferenceException("Invalid type was fed to ConstructAbility");
            }

            return (T)updatable;
        }
    }
    public class ExplodeAbility : MonoUpdatable, IAbilityComponent
    {
        public Ability ab;
        Player player;
        FixTransform playerTransform;
        PlayerBody body;
        PlayerPhysics playerPhysics;
        SpriteRenderer spriteRenderer;
        bool hasFired = true;
        bool releasedButton;
        public void Awake()
        {
            Updater.RegisterUpdatable(this);
            ab = GetComponent<Ability>();
            ab.Cooldown = (Fix)1;
            //playerTransform = base.GetComponent<FixTransform>();
            //body = base.GetComponent<SpriteRenderer>();
            body = GetComponent<PlayerBody>();
            playerPhysics = GetComponent<PlayerPhysics>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            player = PlayerHandler.Get().GetPlayer(ab.GetPlayerInfo().playerId);
        }
        public void Update()
        {

        }


        public override void Init()
        {

        }
        private void OldUpdate(Fix simDeltaTime)
        {
            if(player == null)
            {
                return;
            }
            if (!player.AbilityButtonIsDown(ab.GetPlayerInfo().AbilityButtonUsedIndex012) && !hasFired)
            {
                Fire();
                releasedButton = true;
                hasFired = true;
            }
        }
        public override void UpdateSim(Fix SimDeltaTime)
        {
            OldUpdate(SimDeltaTime);
        }

        public void OnEnterAbility()
        {
            //for sprite
            spriteRenderer.material = ab.GetPlayerInfo().playerMaterial;
            //set right position
            body.position = ab.GetPlayerInfo().position;
            //make gravity work
            playerPhysics.SyncPhysicsTo(ab.GetPlayerInfo());
            //set the player
            player = PlayerHandler.Get().GetPlayer(ab.GetPlayerInfo().playerId);
            //Set bools. One would have been fine, but maybe useful.
            releasedButton = false;
            hasFired = false;
            
        }
        public void Fire()
        {
            //play a sound
            AudioManager.Get().Play("explosion");
            //broadcast info so you can exit the ability normally
            AbilityExitInfo info = default(AbilityExitInfo);
            info.position = body.position;
            info.selfImposedVelocity = body.selfImposedVelocity;
            //call the exit ability function
            ab.ExitAbility(info);
        }

        public void ExitAbility(AbilityExitInfo info)
        {
            enabled = false;
            ab.ExitAbility(info);
        }

        public void OnScaleChanged(Fix scaleMultiplier)
        {
            throw new NotImplementedException();
        }
    }
}
