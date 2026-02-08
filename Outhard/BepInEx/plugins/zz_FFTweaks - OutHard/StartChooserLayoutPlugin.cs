using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FFTweaks.OutHard.UI
{
    [BepInPlugin("com.fierr.fftwaks.ui", "FFTweaks - OutHard UI", "0.1.0")]
    [BepInDependency("com.sinai.SideLoader")]
    public class StartChooserLayoutPlugin : BaseUnityPlugin
    {
        private const string StartChooserUid = "com.iggy.startchooser";
        private const int Columns = 4;

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!string.Equals(scene.name, "DreamWorld", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            StartCoroutine(ApplyLayoutCo());
        }

        private IEnumerator ApplyLayoutCo()
        {
            yield return new WaitForSeconds(0.5f);
            ApplyLayoutOnce();
            yield return new WaitForSeconds(0.5f);
            ApplyLayoutOnce();
        }

        private void ApplyLayoutOnce()
        {
            Type trainerType = AccessTools.TypeByName("CharacterTrainer");
            if (trainerType == null)
            {
                Logger.LogInfo("StartChooser layout: CharacterTrainer type not found.");
                return;
            }

            UnityEngine.Object[] trainers = Resources.FindObjectsOfTypeAll(trainerType);
            int trainerCount = 0;
            int matchedCount = 0;
            int reflowedCount = 0;

            foreach (UnityEngine.Object obj in trainers)
            {
                Component trainer = obj as Component;
                if (trainer == null)
                {
                    continue;
                }

                trainerCount++;

                if (!IsStartChooserTrainer(trainer))
                {
                    continue;
                }

                matchedCount++;

                if (TryReflowTrainer(trainer))
                {
                    reflowedCount++;
                    Logger.LogInfo("StartChooser layout set to 4 columns.");
                }
            }

            Logger.LogInfo(string.Format("StartChooser layout attempt: trainers={0} matched={1} reflowed={2}", trainerCount, matchedCount, reflowedCount));
        }

        private bool IsStartChooserTrainer(Component trainer)
        {
            object character = GetMemberValue(trainer, "Character", "m_character", "m_characterPrefab");
            if (character != null)
            {
                string uid = GetStringMember(character, "UID", "m_UID", "m_uid", "CharacterUID", "UIDString");
                if (string.Equals(uid, StartChooserUid, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                string name = GetStringMember(character, "Name", "m_name");
                if (string.Equals(name, "Soul Guide", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(trainer.name) && trainer.name.IndexOf("Soul Guide", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        private bool TryReflowTrainer(Component trainer)
        {
            object skillTree = GetMemberValue(trainer, "SkillTree", "m_skillTree", "skillTree", "m_trainerTree");
            if (skillTree == null)
            {
                Logger.LogInfo("StartChooser layout: skillTree not found.");
                return false;
            }

            IList slots = FindSkillSlotList(skillTree);
            if (slots == null || slots.Count == 0)
            {
                Logger.LogInfo("StartChooser layout: skill slots not found.");
                return false;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                object slot = slots[i];
                int row = i / Columns;
                int col = i % Columns;
                SetMemberValue(slot, col, "ColumnIndex", "m_columnIndex", "m_column");
                SetMemberValue(slot, row, "RowIndex", "m_rowIndex", "m_row");
            }

            InvokeNoArgIfExists(trainer, "RefreshSkillTree", "Refresh", "BuildSkillTree", "UpdateSkillTree");
            InvokeNoArgIfExists(skillTree, "Refresh", "Build", "Update");
            return true;
        }

        private static IList FindSkillSlotList(object skillTree)
        {
            IList list = FindListByElementName(skillTree, "SkillSlot");
            if (list != null)
            {
                return list;
            }

            IList rowList = FindListByElementName(skillTree, "SkillRow");
            if (rowList == null)
            {
                return null;
            }

            List<object> allSlots = new List<object>();
            foreach (object row in rowList)
            {
                IList slots = FindListByElementName(row, "SkillSlot");
                if (slots == null)
                {
                    continue;
                }
                foreach (object slot in slots)
                {
                    allSlots.Add(slot);
                }
            }

            return allSlots;
        }

        private static IList FindListByElementName(object target, string elementName)
        {
            if (target == null)
            {
                return null;
            }

            Type type = target.GetType();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!typeof(IList).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                IList list = field.GetValue(target) as IList;
                if (list == null || list.Count == 0)
                {
                    continue;
                }

                object first = list[0];
                if (first != null && first.GetType().Name.IndexOf(elementName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return list;
                }
            }

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!typeof(IList).IsAssignableFrom(prop.PropertyType))
                {
                    continue;
                }

                IList list = prop.GetValue(target, null) as IList;
                if (list == null || list.Count == 0)
                {
                    continue;
                }

                object first = list[0];
                if (first != null && first.GetType().Name.IndexOf(elementName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return list;
                }
            }

            return null;
        }

        private static object GetMemberValue(object target, params string[] names)
        {
            if (target == null)
            {
                return null;
            }

            Type type = target.GetType();
            foreach (string name in names)
            {
                PropertyInfo prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null)
                {
                    return prop.GetValue(target, null);
                }

                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field.GetValue(target);
                }
            }

            return null;
        }

        private static string GetStringMember(object target, params string[] names)
        {
            object value = GetMemberValue(target, names);
            return value as string;
        }

        private static void SetMemberValue(object target, int value, params string[] names)
        {
            if (target == null)
            {
                return;
            }

            Type type = target.GetType();
            foreach (string name in names)
            {
                PropertyInfo prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(target, value, null);
                    return;
                }

                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(int))
                {
                    field.SetValue(target, value);
                    return;
                }
            }
        }

        private static void InvokeNoArgIfExists(object target, params string[] names)
        {
            if (target == null)
            {
                return;
            }

            Type type = target.GetType();
            foreach (string name in names)
            {
                MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (method != null)
                {
                    method.Invoke(target, null);
                    return;
                }
            }
        }
    }
}
