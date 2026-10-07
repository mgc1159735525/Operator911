using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        readonly Dictionary<string, Vector2> scrollPositions = new Dictionary<string, Vector2>();
        string redrawKeyboardFocus;
        void RememberKeyboardFocus()
        {
            var events=UnityEngine.EventSystems.EventSystem.current;
            var selected=events==null?null:events.currentSelectedGameObject;
            redrawKeyboardFocus=selected!=null&&root!=null&&selected.transform.IsChildOf(root)?selected.name:null;
        }
        void RestrictKeyboardToTopDialog()
        {
            Transform scope=root;
            // These full-screen raycast shields are siblings in drawing order.
            // The last shield owns keyboard input as well, including nested
            // appearance confirmations and the new-run replacement dialog.
            foreach(Transform child in root)
                if(child.name=="CommandShade"||child.name=="ModalShade"||child.name=="AppearanceShade"||child.name=="LookPopupShade"||child.name=="ReplaceShade"||child.name=="PersistenceFailureShade")scope=child;
            var controls=root.GetComponentsInChildren<Selectable>();
            foreach(var control in controls)
                if(!control.transform.IsChildOf(scope))control.interactable=false;
            var events=UnityEngine.EventSystems.EventSystem.current;if(events==null)return;
            var available=scope.GetComponentsInChildren<Selectable>().Where(control=>control.IsInteractable()).ToList();
            var focus=available.FirstOrDefault(control=>control.name==redrawKeyboardFocus);
            // Returning/continuing is the safe initial focus for a confirmation;
            // Enter must not accidentally accept a destructive action.
            if(focus==null)focus=available.FirstOrDefault(control=>control.name=="LookPopupKeep"||control.name=="CancelNewRun"||control.name.EndsWith("Back")||control.name.EndsWith("Close")||control.name.EndsWith("Cancel"));
            if(focus==null)focus=available.FirstOrDefault();
            events.SetSelectedGameObject(focus==null?null:focus.gameObject);
        }
        void SelectAgent(Agent a)
        {
            selectedAgent = a.id;
            // A selected incident stays focused while comparing people.
            if (string.IsNullOrEmpty(SelectedMission())&&SelectedVisit()==null)
            {
                var task = DispatchRules.Open(State).FirstOrDefault(m => m.id == a.mission);
                if (task != null) { selectedMission = task.id; selectedDistrict = task.district; selectedDuty = (Duty)Enum.Parse(typeof(Duty), task.duty); }
                else { selectedDuty = a.order == Duty.Rest ? Duty.Recon : a.order; selectedDistrict = a.district; rescueTarget = a.target; }
            }
            Draw();
        }
        string AssignmentError()
        {
            var actor=State.agents.First(a=>a.id==selectedAgent);
            if(selectedDuty!=Duty.Rest&&selectedDuty!=Duty.CutContact&&actor.condition==Condition.Active&&DispatchRules.Dice(State,actor,selectedDuty,SelectedMission())<State.config.Action(selectedDuty).required+State.config.District(selectedDistrict).difficulty)return "成功率为0：请换人、核实任务增加骰子，或选择其他目标。";
            // Use exactly the same validator as execution without changing the live state.
            var copy = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));
            return string.IsNullOrEmpty(SelectedMission())
                ? (SelectedVisit()!=null?CityContentRules.AssignSite(copy,selectedAgent,selectedSite):PrototypeRules.Assign(copy, selectedAgent, selectedDuty, rescueTarget, selectedDistrict))
                : DispatchRules.Assign(copy, selectedAgent, SelectedMission());
        }
        void CancelMission(MissionRow m)
        {
            var owner = State.agents.FirstOrDefault(a => a.mission == m.id && a.condition == Condition.Active);
            if (owner == null) return;
            toast = PrototypeRules.Assign(State, owner.id, Duty.Rest);
            if (toast == "") toast = "已撤回「" + m.title + "」，" + owner.name + "将休整；预留资源已释放。";
            Draw();
        }
        void DrawMapMissions(RectTransform panel)
        {
            foreach (var group in DispatchRules.Open(State).GroupBy(m => m.district))
            {
                var d = State.config.District(group.Key); int index = 0;
                foreach (var m in group)
                {
                    var owner = State.agents.FirstOrDefault(a => a.mission == m.id);
                    Button("MapMission_" + m.id, panel, (m.deadline == State.turn ? "急 · " : "待 · ") + m.title,
                        d.x * 868 - 85, d.y * 570 - 67 - index++ * 40, 170, 34,
                        () => SelectMission(m, false), true, SelectedMission() == m.id);
                }
            }
        }
        void RememberScroll()
        {
            if (root == null) return;
            foreach (var scroll in root.GetComponentsInChildren<ScrollRect>()) scrollPositions[scroll.name] = scroll.content.rect.height<=scroll.viewport.rect.height+.5f ? new Vector2(0,1) : scroll.normalizedPosition;
        }
        void RestoreScroll()
        {
            if(resetCommandTaskScroll){scrollPositions.Remove("CommandTasks");resetCommandTaskScroll=false;}
            Canvas.ForceUpdateCanvases();
            foreach (var scroll in root.GetComponentsInChildren<ScrollRect>())
            {
                Vector2 position;
                scroll.normalizedPosition = scrollPositions.TryGetValue(scroll.name, out position) ? position : new Vector2(0,1);
                if(scroll.verticalScrollbar!=null)scroll.verticalScrollbar.gameObject.SetActive(scroll.content.rect.height>scroll.viewport.rect.height+.5f);
            }
        }
    }
}
