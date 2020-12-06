﻿using FMOD;
using HarmonyLib;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if SN1
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
#endif

namespace TechPistol.Module
{
    internal class PistolPrefab : Equipable
    {
		static List<TechType> modelsToMake =  new List<TechType> { TechType.Battery, TechType.PrecursorIonBattery, TechType.PrecursorIonPowerCell, TechType.PowerCell };

		public PistolPrefab() : base("TechPistol", "Tech Pistol", "The Tech Pistol comes with a wide array of functionality including: Explosive Cannon, Laser Pistol, Target Health Detection and the Incredible Resizing Ray")
		{
		}

		private static HashSet<TechType> batteryChargerCompatibleTech => BatteryCharger.compatibleTech;
		private static HashSet<TechType> powerCellChargerCompatibleTech => PowerCellCharger.compatibleTech;
		private static List<TechType> compatibleTech => batteryChargerCompatibleTech.Concat(powerCellChargerCompatibleTech).ToList();

		public override EquipmentType EquipmentType => EquipmentType.Hand;
        public override Vector2int SizeInInventory => new Vector2int(2,2);
		public override TechGroup GroupForPDA => TechGroup.Personal;
        public override TechCategory CategoryForPDA => TechCategory.Tools;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
        public override string[] StepsToFabricatorTab => new string[] { "Personal", "Tools" };
        public override float CraftingTime => 5f;
        public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;

		public override TechType RequiredForUnlock => Main.pistolFragment.TechType;

		public override string DiscoverMessage => $"{this.FriendlyName} Unlocked!";

		public override bool AddScannerEntry => true;

		public override int FragmentsToScan => 5;

		public override float TimeToScanFragment => 5f;

		public override bool DestroyFragmentOnScan => true;

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> pistol)
		{
			GameObject prefab = Main.assetBundle.LoadAsset<GameObject>("TechPistol.prefab");
			GameObject gameObject = GameObject.Instantiate(prefab);
			gameObject.SetActive(false);
			prefab.SetActive(false);
			MeshRenderer[] componentsInChildren = gameObject.transform.Find("HandGun").gameObject.GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer meshRenderer in componentsInChildren)
			{
				if (meshRenderer.name.StartsWith("Gun") || meshRenderer.name.StartsWith("Target"))
				{
					Texture emissionMap = meshRenderer.material.GetTexture("_EmissionMap");
					Texture specMap = meshRenderer.material.GetTexture("_MetallicGlossMap");

					meshRenderer.material.shader = Shader.Find("MarmosetUBER");
					meshRenderer.material.EnableKeyword("MARMO_EMISSION");
					meshRenderer.material.EnableKeyword("MARMO_SPECMAP");
					meshRenderer.material.SetTexture(ShaderPropertyID._Illum, emissionMap);
					meshRenderer.material.SetTexture(ShaderPropertyID._SpecTex, specMap);
					meshRenderer.material.SetColor("_GlowColor", new Color(1f, 1f, 1f));
					meshRenderer.material.SetFloat(ShaderPropertyID._GlowStrength, 1f);
					meshRenderer.material.SetFloat(ShaderPropertyID._GlowStrengthNight, 1f);
				}
			}

			SkyApplier skyApplier = gameObject.EnsureComponent<SkyApplier>();
			skyApplier.renderers = componentsInChildren;
			skyApplier.anchorSky = Skies.Auto;

			gameObject.EnsureComponent<PrefabIdentifier>().ClassId = base.ClassID;
			gameObject.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.Near;
			gameObject.EnsureComponent<TechTag>().type = base.TechType;


			EnergyMixin energyMixin = gameObject.EnsureComponent<EnergyMixin>();
			energyMixin.storageRoot = gameObject.transform.Find("HandGun/GunMain/BatteryRoot").gameObject.EnsureComponent<ChildObjectIdentifier>();
			gameObject.transform.Find("HandGun/GunMain/BatteryRoot/Battery")?.gameObject.SetActive(false);
			energyMixin.allowBatteryReplacement = true;
			energyMixin.compatibleBatteries = compatibleTech;

			List<EnergyMixin.BatteryModels> batteryModels = new List<EnergyMixin.BatteryModels>();
			Transform BatteryRoot = gameObject.transform.Find(PistolBehaviour.GunMain + "/BatteryRoot");

			foreach (TechType techType in modelsToMake)
			{
				CoroutineTask<GameObject> batteryTask = CraftData.GetPrefabForTechTypeAsync(techType, false);
				yield return batteryTask;

				GameObject batteryprefab = batteryTask.GetResult();
				GameObject model = GameObject.Instantiate(batteryprefab);
				batteryprefab.SetActive(false);
				model.SetActive(false);

				GameObject.DestroyImmediate(model.GetComponentInChildren<WorldForces>());
				GameObject.DestroyImmediate(model.GetComponentInChildren<Rigidbody>());
				GameObject.DestroyImmediate(model.GetComponentInChildren<Battery>());
				GameObject.DestroyImmediate(model.GetComponentInChildren<LargeWorldEntity>());
				GameObject.DestroyImmediate(model.GetComponentInChildren<TechTag>());
				GameObject.DestroyImmediate(model.GetComponentInChildren<EntityTag>());
				GameObject.DestroyImmediate(model.GetComponentInChildren<Pickupable>());
				GameObject.DestroyImmediate(model.GetComponentInChildren<Collider>());

				if (model.TryGetComponent<PrefabIdentifier>(out PrefabIdentifier prefabIdentifier))
				{
					string classId = prefabIdentifier.ClassId;
					GameObject.DestroyImmediate(prefabIdentifier);
					ChildObjectIdentifier childObjectIdentifier = model.EnsureComponent<ChildObjectIdentifier>();
					childObjectIdentifier.ClassId = classId;
				}

				model.SetActive(false);

				bool cellCheck = techType.AsString().ToLower().Contains("cell");

				Vector3 position = cellCheck ? new Vector3(0f, 1.45f, 0.95f) : new Vector3(0f, 1.46f, 0.95f);
				Vector3 scale = cellCheck ? new Vector3(0.15f, 0.15f, 0.15f) : new Vector3(0.3f, 0.3f, 0.3f);

				model.transform.SetParent(BatteryRoot);
				model.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
				model.transform.localPosition = position;
				model.transform.localEulerAngles = new Vector3(270f, 0f, 0f);
				model.transform.localScale = scale;

				batteryModels.Add(new EnergyMixin.BatteryModels
				{
					techType = techType,
					model = model
				});
			}
			energyMixin.batteryModels = batteryModels.ToArray();

			CoroutineTask<GameObject> task1 = CraftData.GetPrefabForTechTypeAsync(TechType.RepulsionCannon, false);
			yield return task1;
			GameObject gameObject1 = task1.GetResult();
			gameObject1.SetActive(false);
			RepulsionCannon component = gameObject1.GetComponent<RepulsionCannon>();

			CoroutineTask<GameObject> task2 = CraftData.GetPrefabForTechTypeAsync(TechType.StasisRifle, false);
			yield return task2;
			GameObject gameObject2 = task2.GetResult();
			gameObject2.SetActive(false);
			StasisRifle component2 = gameObject2.GetComponent<StasisRifle>();

			CoroutineTask<GameObject> task3 = CraftData.GetPrefabForTechTypeAsync(TechType.PropulsionCannon, false);
			yield return task3;
			GameObject gameObject3 = task3.GetResult();
			gameObject3.SetActive(false);
			PropulsionCannon component3 = gameObject3.GetComponent<PropulsionCannon>();

			CoroutineTask<GameObject> task4 = CraftData.GetPrefabForTechTypeAsync(TechType.Welder, false);
			yield return task4;
			GameObject gameObject4 = task4.GetResult();
			gameObject4.SetActive(false);
			Welder component4 = gameObject4.GetComponent<Welder>();
			
			PistolBehaviour pistolBehaviour = gameObject.EnsureComponent<PistolBehaviour>();
			pistolBehaviour.repulsionCannonFireSound = component.shootSound;
			pistolBehaviour.stasisRifleFireSound = component2.fireSound;
			pistolBehaviour.stasisRifleEvent = component2.chargeBegin;
			pistolBehaviour.modeChangeSound = component3.shootSound;
			pistolBehaviour.laserShootSound = component4.weldSound;
			pistolBehaviour.ikAimRightArm = true;
			pistolBehaviour.hasBashAnimation = true;
			Pickupable pickupable = gameObject.EnsureComponent<Pickupable>();
			pickupable.isPickupable = true;
			pistolBehaviour.pickupable = pickupable;

			pistol.Set(gameObject);
			yield break;
		}

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>
				{
					new Ingredient(TechType.TitaniumIngot, 1),
					new Ingredient(TechType.Lubricant, 1),
					new Ingredient(TechType.AdvancedWiringKit, 1),
					new Ingredient(TechType.EnameledGlass, 1)
				}
			};
		}

		protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromTexture(Main.assetBundle.LoadAsset<Texture2D>("Icon"));
        }
    }
}
