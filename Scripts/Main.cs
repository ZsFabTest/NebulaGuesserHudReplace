using Nebula;
using Nebula.Modules;
using Nebula.Utilities;
using Virial.Assignable;
using HarmonyLib;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Nebula;

public class GuessHudReplaceAddon{
    static GuessHudReplaceAddon(){
        string[] simpleSwitch = ["options.switch.off", "options.switch.on"];
        NebulaPlugin.Harmony.Patch(typeof(Nebula.Roles.Complex.MeetingRoleSelectWindow).GetMethod("OpenRoleSelectWindow", BindingFlags.Static | BindingFlags.Public), new HarmonyMethod(typeof(GuessHudReplacePatch).GetMethod("OpenRoleSelectWindowPatch")));
        new ClientOption((ClientOption.ClientOptionType)114, "EnableGuessHudPatch", simpleSwitch, 1);
    }
}

public static class GuessHudReplacePatch{
    public static bool OpenRoleSelectWindowPatch(MetaScreen __result, [HarmonyArgument(0)] Func<DefinedRole, bool> predicate, [HarmonyArgument(1)] string underText, [HarmonyArgument(2)] Action<DefinedRole> onSelected)
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)114) == 0) return true;
        Transform container = UnityEngine.Object.Instantiate(MeetingHud.Instance.transform.Find("MeetingContents/PhoneUI"), MeetingHud.Instance.transform);
        container.transform.localPosition = new Vector3(0, 0, -50f);
        var guesserUI = container.gameObject;
        var window = MetaScreen.GenerateWindow(new Vector2(1f, 1f), container, new Vector3(0, 0, -50f), true, true);

        var buttonTemplate = MeetingHud.Instance.playerStates[0].transform.Find("votePlayerBase");
        //var maskTemplate = MeetingHud.Instance.playerStates[0].transform.Find("MaskArea");
        var smallButtonTemplate = MeetingHud.Instance.playerStates[0].Buttons.transform.Find("CancelButton");
        var textTemplate = MeetingHud.Instance.playerStates[0].NameText;

        Transform exitButtonParent = (new GameObject()).transform;
        exitButtonParent.SetParent(container);
        Transform exitButton = UnityEngine.Object.Instantiate(buttonTemplate, exitButtonParent);
        //Transform exitButtonMask = UnityEngine.Object.Instantiate(maskTemplate, exitButtonParent);
        exitButton.gameObject.GetComponent<SpriteRenderer>().sprite = smallButtonTemplate.GetComponent<SpriteRenderer>().sprite;
        exitButtonParent.transform.localPosition = new Vector3(2.5f, 2.1f, -5);
        exitButtonParent.transform.localScale = new Vector3(0.25f, 0.9f, 1);
        exitButton.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
        exitButton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            MeetingHud.Instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
            UnityEngine.Object.Destroy(container.gameObject);
        }));

        List<Transform> buttons = new List<Transform>();
        Transform selectedButton = null;

        for (int j = 0; j < 3; j++){
            Transform buttonParent = (new GameObject()).transform;
            buttonParent.SetParent(container);
            Transform button = UnityEngine.Object.Instantiate(buttonTemplate, buttonParent);
            //Transform buttonMask = UnityEngine.Object.Instantiate(maskTemplate, buttonParent);
            TMPro.TextMeshPro label = UnityEngine.Object.Instantiate(textTemplate, button);
            buttonParent.localPosition = new Vector3(-2.5f + 1.5f * j, 1.5f, -5f);
            buttonParent.localScale = new Vector3(0.5f, 0.5f, 1f);
            label.text = Language.Translate($"guesserHud.text{j}");
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
            label.transform.localScale *= 1.7f;
            int copiedJ = j;

            button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
            if (!PlayerControl.LocalPlayer.Data.IsDead) button.GetComponent<PassiveButton>().OnClick.AddListener((System.Action)(() => {
                LoadPage((role) => ((int)role.Category) == (1 << copiedJ));
            }));
        }

        void LoadPage(Func<DefinedRole, bool> pagePredicate){
            foreach(var button in buttons){
                UnityEngine.Object.Destroy(button.gameObject);
            }

            buttons.Clear();
            selectedButton = null;

            int i = 0;
            foreach (DefinedRole role in Roles.Roles.AllRoles.Where(predicate).Where(pagePredicate))
            {
                Transform buttonParent = (new GameObject()).transform;
                buttonParent.SetParent(container);
                Transform button = UnityEngine.Object.Instantiate(buttonTemplate, buttonParent);
                //Transform buttonMask = UnityEngine.Object.Instantiate(maskTemplate, buttonParent);
                TMPro.TextMeshPro label = UnityEngine.Object.Instantiate(textTemplate, button);
                buttons.Add(button);
                int row = i / 6, col = i % 6;
                buttonParent.localPosition = new Vector3(-3.5f + 1.4f * col, 1.1f - 0.37f * row, -5);
                buttonParent.localScale = new Vector3(0.5f, 0.5f, 1f);
                label.text = role.DisplayColoredName;
                label.alignment = TMPro.TextAlignmentOptions.Center;
                label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
                label.transform.localScale *= 1.7f;
                int copiedIndex = i;

                button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
                if (!PlayerControl.LocalPlayer.Data.IsDead) button.GetComponent<PassiveButton>().OnClick.AddListener((System.Action)(() =>
                {
                    if (selectedButton != button)
                    {
                        selectedButton = button;
                        buttons.ForEach(x => x.GetComponent<SpriteRenderer>().color = x == selectedButton ? Color.red : Color.white);
                    }
                    else
                    {
                        if (PlayerControl.LocalPlayer.Data.IsDead){
                            return;
                        }
                        MeetingHud.Instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                        UnityEngine.Object.Destroy(container.gameObject);
                        onSelected.Invoke(role);
                    }
                }));

                i++;
            }
        }

        LoadPage((role) => ((int)role.Category) == 1);

        window.CloseScreen();
        __result = window;
        return false;
    }
}