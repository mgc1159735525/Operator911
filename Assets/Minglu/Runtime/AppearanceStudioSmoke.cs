using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        Button StudioButton(string name)
        {
            foreach(var button in root.GetComponentsInChildren<Button>())if(button.name==name)return button;
            throw new Exception("Portrait control missing: "+name);
        }
        // Exercise the visible selector instead of setting draft fields directly.
        // Other portrait smoke journeys share this helper, so catalog growth
        // does not require hard-coded option buttons or page numbers.
        IEnumerator StudioSetOption(int category,int option)
        {
            if(category==1||category==13)throw new Exception("Removed hair/ear controls must not be exercised");
            int group=category>=2&&category<=6?1:0;
            if(lookGroup!=group)yield return Click("LookGroup_"+group);
            if(option<0||option>=LookOptions(category).Length)throw new Exception("Portrait option out of range: "+category+" / "+option);
            yield return Click("LookCategory_"+category);
            int remaining=LookOptions(category).Length;
            while(LookIndex(category)!=option&&remaining-->0)yield return Click("LookNext_"+category);
            if(LookIndex(category)!=option)throw new Exception("Portrait cycle did not reach option: "+category+" / "+option);
        }
        IEnumerator StudioSetLock(int category,bool locked)
        {
            if(category==1||category==13)throw new Exception("Removed hair/ear channels cannot be locked");
            if(lookLocks[category]==locked)yield break;
            yield return Click("LookRandomSettings");yield return Click("LookRandomLock_"+category);yield return Click("LookPopupBack");
            if(lookLocks[category]!=locked)throw new Exception("Random setting did not retain the requested lock");
        }
        IEnumerator StudioSetSex(int sex)
        {
            if(lookGroup!=0)yield return Click("LookGroup_0");
            yield return Click(sex==0?"LookMale":"LookFemale");
        }
        string classicPixelSample="",classicPixelPhase="";
        Color32[] classicPixelBuffer;
        RectInt classicPixelBounds;
        ulong ClassicPreviewSignature()
        {
            Texture2D pixels=null;
            var target=new RenderTexture(Screen.width,Screen.height,24);
            var previousTarget=viewCamera.targetTexture;var previousActive=RenderTexture.active;
            try
            {
                // A nested Click coroutine can resume its caller outside the
                // drawing frame, even after its own WaitForEndOfFrame. Never
                // read the system framebuffer here: explicitly render the
                // same game camera and UI into an owned target, as Capture does.
                target.Create();viewCamera.targetTexture=target;Canvas.ForceUpdateCanvases();
                var preview=GameObject.Find("StudioPreview");
                if(preview==null)throw new Exception("Classic portrait preview is missing");
                var corners=new Vector3[4];((RectTransform)preview.transform).GetWorldCorners(corners);
                var low=RectTransformUtility.WorldToScreenPoint(viewCamera,corners[0]);var high=RectTransformUtility.WorldToScreenPoint(viewCamera,corners[2]);
                // At 0.8 UI scale an exact boundary such as 544 can project as
                // 543.99994. Stabilize only this subpixel rounding, never RGB.
                const float edgeEpsilon=.001f;
                int x=Mathf.Clamp(Mathf.CeilToInt(low.x-edgeEpsilon)+2,0,Screen.width-1),y=Mathf.Clamp(Mathf.CeilToInt(low.y-edgeEpsilon)+2,0,Screen.height-1);
                int w=Mathf.Min(Screen.width-x,Mathf.FloorToInt(high.x+edgeEpsilon)-x-2),h=Mathf.Min(Screen.height-y,Mathf.FloorToInt(high.y+edgeEpsilon)-y-2);
                if(w<40||h<60)throw new Exception("Classic preview is clipped outside the actual game viewport");
                classicPixelSample=x+","+y+","+w+","+h+" aspect="+viewCamera.aspect;
                pixels=new Texture2D(w,h,TextureFormat.RGBA32,false);
                viewCamera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new UnityEngine.Rect(x,y,w,h),0,0,false);pixels.Apply(false,false);
                classicPixelBuffer=pixels.GetPixels32();classicPixelBounds=new RectInt(x,y,w,h);
                ulong hash=1469598103934665603UL;int lowest=255,highest=0;
                foreach(var c in classicPixelBuffer)
                {
                    int luminance=(c.r*3+c.g*6+c.b)/10;lowest=Math.Min(lowest,luminance);highest=Math.Max(highest,luminance);
                    unchecked{hash=(hash^c.r)*1099511628211UL;hash=(hash^c.g)*1099511628211UL;hash=(hash^c.b)*1099511628211UL;}
                }
                if(highest-lowest<8)throw new Exception("Classic preview pixel sample contains no rendered detail: rect="+x+","+y+","+w+","+h+" range="+lowest+".."+highest);
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"--studio-cycle-probe")>=0&&lookDraft.sex==0&&lookDraft.parts[0]==0)
                {
                    // Preserve the buffer actually hashed, not a second
                    // screenshot that can hide a transient render state.
                    var args=Environment.GetCommandLineArgs();int capture=Array.IndexOf(args,"--minglu-capture");
                    string prefix=Path.ChangeExtension(capture>=0&&capture+1<args.Length?args[capture+1]:smokeSave,null);
                    string diagnostic=prefix+".pixel-buffer-"+hash+".png";File.WriteAllBytes(diagnostic,pixels.EncodeToPNG());
                    ((RectTransform)preview.transform).GetWorldCorners(corners);
                    var afterLow=RectTransformUtility.WorldToScreenPoint(viewCamera,corners[0]);var afterHigh=RectTransformUtility.WorldToScreenPoint(viewCamera,corners[2]);
                    Debug.Log("MINGLU_CLASSIC_PIXEL_BUFFER: phase="+classicPixelPhase+", hash="+hash+", sample="+classicPixelSample+", projectedBefore="+low.ToString("F5")+".."+high.ToString("F5")+", projectedAfter="+afterLow.ToString("F5")+".."+afterHigh.ToString("F5")+", file="+diagnostic);
                }
                return hash;
            }
            finally
            {
                RenderTexture.active=previousActive;viewCamera.targetTexture=previousTarget;viewCamera.ResetAspect();viewCamera.ResetProjectionMatrix();
                target.Release();Destroy(target);if(pixels!=null)Destroy(pixels);
            }
        }
        static void ClassicPixelDifference(Color32[] expected,Color32[] actual,out int changed,out int maxDelta,out int visible)
        {
            if(expected.Length!=actual.Length)throw new Exception("Portrait preview buffer dimensions changed");
            changed=0;maxDelta=0;visible=0;
            for(int i=0;i<expected.Length;i++)
            {
                int delta=Math.Max(Math.Abs(expected[i].r-actual[i].r),Math.Max(Math.Abs(expected[i].g-actual[i].g),Math.Abs(expected[i].b-actual[i].b)));
                if(delta>0)changed++;if(delta>1)visible++;maxDelta=Math.Max(maxDelta,delta);
            }
        }
        void SaveClassicPixelEvidence(string path,Color32[] buffer,RectInt bounds)
        {
            var texture=new Texture2D(bounds.width,bounds.height,TextureFormat.RGBA32,false);
            try{texture.SetPixels32(buffer);texture.Apply(false,false);File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{Destroy(texture);}
        }
        Exception ClassicPixelFailure(int category,int option,string reason,ulong expected,ulong actual,Color32[] expectedBuffer,RectInt expectedBounds)
        {
            var args=Environment.GetCommandLineArgs();int capture=Array.IndexOf(args,"--minglu-capture");
            string path=Path.ChangeExtension(capture>=0&&capture+1<args.Length?args[capture+1]:smokeSave,"pixels-failed-c"+category+"-o"+option+".png");
            SaveClassicPixelEvidence(Path.ChangeExtension(path,"expected-"+expected+".png"),expectedBuffer,expectedBounds);
            SaveClassicPixelEvidence(Path.ChangeExtension(path,"actual-"+actual+".png"),classicPixelBuffer,classicPixelBounds);
            Capture(path);
            return new Exception(reason+": sex="+lookDraft.sex+", category="+category+", option="+option+", selected="+LookIndex(category)+", expectedHash="+expected+", actualHash="+actual+", sample="+classicPixelSample+", screenshot="+path+", draft="+JsonUtility.ToJson(lookDraft));
        }
        IEnumerator CheckStudioCycle(int category)
        {
            // Face selection intentionally defaults elders to silver. Lock the
            // colour through its visible control for this independence test;
            // GroomingSmoke separately tests the age default and override.
            bool restoreColorLock=category==0&&!lookLocks[12];
            if(restoreColorLock)yield return StudioSetLock(12,true);
            yield return StudioSetOption(category,0);
            int count=LookOptions(category).Length;
            var before=AppearanceRules.Copy(lookDraft);
            // Click waits before invoking its callback. Let the replacement
            // canvas reach a drawing frame before sampling the new state.
            yield return null;yield return new WaitForEndOfFrame();
            classicPixelPhase="category-"+category+"-initial";
            ulong initialPixels=ClassicPreviewSignature();var pixelVariants=new HashSet<ulong>{initialPixels};
            var initialBuffer=classicPixelBuffer;var initialBounds=classicPixelBounds;
            var variantBuffers=new List<Color32[]>{initialBuffer};var variantHashes=new List<ulong>{initialPixels};
            yield return Click("LookPrevious_"+category);
            if(LookIndex(category)!=count-1)throw new Exception("Portrait previous did not wrap: "+category);
            yield return Click("LookCurrent_"+category);
            if(JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(before))throw new Exception("Portrait current-name click did not wrap exactly: "+category);
            if(EventSystem.current.currentSelectedGameObject==null||EventSystem.current.currentSelectedGameObject.name!="LookCurrent_"+category)throw new Exception("Portrait cycle lost keyboard focus after redraw: "+category);
            for(int option=1;option<=count;option++)
            {
                yield return Click("LookNext_"+category);
                var expected=AppearanceRules.Copy(before);
                expected.parts[category]=option%count;
                if(JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(expected))throw new Exception("Portrait cycle changed another part or skipped an option: "+category+" / "+option);
                yield return null;yield return new WaitForEndOfFrame();
                classicPixelPhase="category-"+category+"-option-"+option;
                ulong rendered=ClassicPreviewSignature();
                if(!classicPixelBounds.Equals(initialBounds))throw ClassicPixelFailure(category,option,"Portrait preview sample changed position or dimensions",initialPixels,rendered,initialBuffer,initialBounds);
                if(option<count)
                {
                    for(int prior=0;prior<variantBuffers.Count;prior++)
                    {
                        int changed,maxDelta,visible;ClassicPixelDifference(variantBuffers[prior],classicPixelBuffer,out changed,out maxDelta,out visible);
                        if(visible<32)throw ClassicPixelFailure(category,option,"Authored portrait choice is not visibly distinct from option "+prior+"; RGB pixels with delta>1="+visible+", changed="+changed+", maxDelta="+maxDelta,variantHashes[prior],rendered,variantBuffers[prior],initialBounds);
                    }
                    pixelVariants.Add(rendered);variantBuffers.Add(classicPixelBuffer);variantHashes.Add(rendered);
                }
                else
                {
                    int changed,maxDelta,visible;ClassicPixelDifference(initialBuffer,classicPixelBuffer,out changed,out maxDelta,out visible);
                    // Two captured 720p buffers differed at exactly one RGB
                    // channel by 1/255 in a collar pixel. Permit only this
                    // bounded GPU quantization, never shifted/changed art.
                    int roundoffBudget=Math.Max(2,classicPixelBuffer.Length/10000);
                    if(changed>roundoffBudget||maxDelta>1)throw ClassicPixelFailure(category,option,"Portrait cycle returned its data but not its original pixels; changed="+changed+", maxDelta="+maxDelta,initialPixels,rendered,initialBuffer,initialBounds);
                    if(changed>0)Debug.Log("MINGLU_CLASSIC_PIXEL_QUANTIZATION: sex="+lookDraft.sex+", category="+category+", changed="+changed+"/"+classicPixelBuffer.Length+", maxRGBDelta="+maxDelta+", limit="+roundoffBudget+" pixels (0.01%) and 1/255 per channel, exact sample="+classicPixelBounds);
                }
            }
            var current=StudioButton("LookCurrent_"+category);EventSystem.current.SetSelectedGameObject(current.gameObject);
            ExecuteEvents.Execute(current.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            yield return null;yield return new WaitForEndOfFrame();
            if(LookIndex(category)!=1||EventSystem.current.currentSelectedGameObject==null||EventSystem.current.currentSelectedGameObject.name!="LookCurrent_"+category)throw new Exception("Portrait keyboard Submit failed or lost focus: "+category);
            yield return Click("LookPrevious_"+category);
            if(restoreColorLock){yield return StudioSetLock(12,false);yield return StudioSetOption(category,0);}
            Debug.Log("MINGLU_CLASSIC_PIXEL_CYCLE: sex="+lookDraft.sex+", category="+category+", distinct="+pixelVariants.Count+", expected="+count+", preview-only render, each choice differs by >=32 RGB pixels above delta1; exact data/rect wrap, <=0.01% pixels RGB delta1 GPU allowance");
        }
        void CheckStudioPanel(string name)
        {
            Canvas.ForceUpdateCanvases();var panel=GameObject.Find(name).GetComponent<RectTransform>();
            var corners=new Vector3[4];
            foreach(var button in panel.GetComponentsInChildren<Button>())
            {
                ((RectTransform)button.transform).GetWorldCorners(corners);
                foreach(var world in corners)
                {
                    var local=panel.InverseTransformPoint(world);
                    if(local.x<panel.rect.xMin-1||local.x>panel.rect.xMax+1||local.y<panel.rect.yMin-1||local.y>panel.rect.yMax+1)throw new Exception("Portrait control extends beyond its panel: "+button.name);
                }
            }
            foreach(var label in panel.GetComponentsInChildren<Text>())
            {
                if(label.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(label.text)&&label.verticalOverflow==VerticalWrapMode.Truncate&&label.preferredHeight>label.rectTransform.rect.height+2)
                    throw new Exception("Portrait text is clipped: "+label.text);
            }
        }
        IEnumerator RequireStudioPopupBlocksEditor()
        {
            // Draw replaces the canvas hierarchy in the click handler. Wait for
            // a rendered frame, as Click does, before inspecting graphic depth.
            yield return null;yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();
            var button=StudioButton("LookRandom");var rt=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(viewCamera,rt.TransformPoint(rt.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()==button)throw new Exception("Portrait popup does not block editor: hits="+hits.Count+", first="+(hits.Count==0?"none":hits[0].gameObject.name));
        }
        IEnumerator CheckClassicWriteFailure()
        {
            if(string.IsNullOrEmpty(smokeSave)||!atTitle||homePage!="appearance")throw new Exception("Portrait save-failure fixture requires the isolated title editor");
            string originalPath=smokeSave,temporary=smokeSave+".classic-write-failure";
            string live=JsonUtility.ToJson(profile),draft=JsonUtility.ToJson(lookDraft);
            var occupied=new[]{HasLookSlot(0),HasLookSlot(1),HasLookSlot(2)};
            if(LookSlotHasContent(new Appearance())||!LookSlotHasContent(AppearanceRules.AsRealistic(null)))throw new Exception("Empty serialized slot and saved current default are not distinguished");
            string blocked=temporary+".profile";
            if(File.Exists(blocked)||Directory.Exists(blocked))throw new Exception("Portrait write-failure fixture already exists");
            try
            {
                // A directory at the destination file makes the isolated
                // write fail without changing permissions or a player file.
                smokeSave=temporary;Directory.CreateDirectory(blocked);
                yield return Click("LookPresets");yield return Click("LookSaveSlot");if(lookPopup=="overwrite")yield return Click("LookPopupConfirm");
                if(lookPopup!="presetError"||JsonUtility.ToJson(profile)!=live||JsonUtility.ToJson(lookDraft)!=draft)throw new Exception("Failed preset write was hidden or changed live data/draft");
                yield return RequireStudioPopupBlocksEditor();RequireKeyboardScope("LookPopupShade");yield return Click("LookPopupKeep");
                yield return Click("LookApply");
                if(lookPopup!="applyError"||homePage!="appearance"||JsonUtility.ToJson(profile)!=live||JsonUtility.ToJson(lookDraft)!=draft)throw new Exception("Failed appearance apply closed the editor or changed live data/draft");
                yield return RequireStudioPopupBlocksEditor();RequireKeyboardScope("LookPopupShade");
                Directory.Delete(blocked);yield return Click("LookPopupRetry");
                if(homePage!="main"||JsonUtility.ToJson(profile.chiefLook)!=draft||JsonUtility.ToJson(RunLifecycle.ReadProfile(ProfilePath).chiefLook)!=draft)throw new Exception("Portrait save retry failed to persist the preserved draft");
                var persisted=RunLifecycle.ReadProfile(ProfilePath);
                for(int slot=0;slot<occupied.Length;slot++)if(HasLookSlot(slot)!=occupied[slot]||LookSlotHasContent(persisted.lookSlots[slot])!=occupied[slot])throw new Exception("Saving chief appearance changed preset slot occupancy: "+slot);
                yield return Click("HomeAppearance");
            }
            finally
            {
                smokeSave=originalPath;
                if(Directory.Exists(blocked))Directory.Delete(blocked);
                foreach(string file in new[]{temporary,temporary+".bak",temporary+".tmp",blocked,blocked+".bak",blocked+".tmp"})if(File.Exists(file))File.Delete(file);
            }
            Debug.Log("MINGLU_CLASSIC_SAVE_FAILURE_UI_SUCCESS: preset/apply failures preserve live data and draft, error dialogs block pointer/keyboard, retry persists unchanged draft; isolated fault paths cleaned");
        }
        IEnumerator AppearanceStudioSmoke(string path)
        {
            Capture(Path.ChangeExtension(path,"home.png"));yield return Click("HomeNew");CheckStudioPanel("HomePanel");Capture(Path.ChangeExtension(path,"new-run.png"));
            yield return Click("HomeBack");yield return Click("HomeAppearance");CheckStudioPanel("AppearanceStudio");Capture(Path.ChangeExtension(path,"editor-initial.png"));
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--studio-cycle-probe")>=0)
            {
                yield return Click("LookMale");yield return CheckStudioCycle(0);
                Debug.Log("MINGLU_CLASSIC_PIXEL_PROBE_SUCCESS: first head cycle passed distinct previews and bounded pixel wrap");yield break;
            }
            string untouched=JsonUtility.ToJson(lookDraft);int untouchedUndo=lookUndo.Count;
            yield return Click("LookRandomSettings");yield return Click("LookLockAll");yield return Click("LookRandomFromSettings");
            if(JsonUtility.ToJson(lookDraft)!=untouched||lookUndo.Count!=untouchedUndo||LookDirty())throw new Exception("First-use locked random marked an untouched appearance dirty");
            yield return Click("LookRandomSettings");yield return Click("LookUnlockAll");yield return Click("LookPopupBack");
            yield return Click("LookMale");
            var studio=GameObject.Find("AppearanceStudio");if(studio==null||!PaperSurface(studio.transform))throw new Exception("Portrait editor is not a paper surface");
            foreach(int category in new[]{0,7,8,10,11,12,9})yield return CheckStudioCycle(category);
            yield return Click("LookGroup_1");
            foreach(int category in new[]{2,3,4,5,6})
            {
                StudioButton("LookCurrent_"+category);
                yield return CheckStudioCycle(category);
            }
            int shown=0;foreach(var button in root.GetComponentsInChildren<Button>())if(button.name.StartsWith("LookCurrent_"))shown++;
            if(shown!=5)throw new Exception("Male facial parts are not immediately accessible in five cycle rows");
            foreach(var button in root.GetComponentsInChildren<Button>())if(button.name=="LookGroup_2"||button.name=="LookCurrent_1"||button.name=="LookCurrent_13")throw new Exception("Removed legacy portrait control remains visible");
            CheckStudioPanel("AppearanceStudio");Capture(Path.ChangeExtension(path,"facial-cycles.png"));
            yield return StudioSetOption(0,LookOptions(0).Length-1);if(LookIndex(0)!=LookOptions(0).Length-1)throw new Exception("Last face is not selectable");
            yield return StudioSetOption(0,0);
            string beforeSettings=JsonUtility.ToJson(lookDraft);yield return Click("LookRandomSettings");yield return Click("LookRandomLock_0");
            if(lookPopup!="random"||!lookLocks[0]||JsonUtility.ToJson(lookDraft)!=beforeSettings)throw new Exception("Random settings changed character instead of only preferences");
            CheckStudioPanel("LookPopup");yield return RequireStudioPopupBlocksEditor();Capture(Path.ChangeExtension(path,"random-settings.png"));
            yield return Click("LookLockAll");foreach(int category in LookEditableCategories())if(!lookLocks[category])throw new Exception("Lock all omitted an editable component");
            string fullyLocked=JsonUtility.ToJson(lookDraft);int lockedUndoCount=lookUndo.Count;
            yield return Click("LookRandomFromSettings");
            if(lookPopup!=""||JsonUtility.ToJson(lookDraft)!=fullyLocked||lookUndo.Count!=lockedUndoCount)throw new Exception("Fully locked random changed appearance or added an undo entry");
            yield return Click("LookRandomSettings");
            yield return Click("LookUnlockAll");foreach(int category in LookEditableCategories())if(lookLocks[category])throw new Exception("Unlock all retained an editable component");
            yield return Click("LookPopupBack");
            if(lookPopup!=""||JsonUtility.ToJson(lookDraft)!=beforeSettings)throw new Exception("Returning from random settings mutated preview");
            yield return Click("LookRandomSettings");yield return Click("LookRandomFromSettings");if(lookPopup!="")throw new Exception("Random and view did not return to editor");
            yield return Click("LookUndo");if(JsonUtility.ToJson(lookDraft)!=beforeSettings)throw new Exception("Random settings could not be undone exactly");
            yield return StudioSetOption(0,2);
            int head=lookDraft.parts[0];yield return StudioSetLock(0,true);string before=JsonUtility.ToJson(lookDraft);yield return Click("LookRandom");
            if(lookDraft.parts[0]!=head||lookDraft.sex!=0)throw new Exception("Locked random changed complete head or sex");string after=JsonUtility.ToJson(lookDraft);
            yield return Click("LookUndo");if(JsonUtility.ToJson(lookDraft)!=before)throw new Exception("Undo not exact");yield return Click("LookRedo");if(JsonUtility.ToJson(lookDraft)!=after)throw new Exception("Redo not exact");
            yield return StudioSetOption(0,1);if(lookDraft.parts[0]!=1||!lookLocks[0])throw new Exception("Random lock incorrectly prevented explicit head change");
            yield return StudioSetOption(6,3);CheckStudioPanel("AppearanceStudio");Capture(Path.ChangeExtension(path,"male-full.png"));
            yield return Click("LookZoom");Capture(Path.ChangeExtension(path,"male-detail.png"));
            yield return StudioSetSex(1);if(lookDraft.parts[6]!=0)throw new Exception("Female retains hidden beard");yield return StudioSetOption(0,1);
            yield return Click("LookGroup_1");
            foreach(int category in new[]{2,3,4,5})yield return CheckStudioCycle(category);
            foreach(var button in root.GetComponentsInChildren<Button>())if(button.name=="LookCurrent_6")throw new Exception("Female editor exposes a beard row");
            Capture(Path.ChangeExtension(path,"female-detail.png"));
            yield return Click("LookZoom");CheckStudioPanel("AppearanceStudio");Capture(Path.ChangeExtension(path,"female-full.png"));
            yield return Click("LookCompare");if(!lookCompare||StudioButton("LookApply").interactable)throw new Exception("Original comparison allows applying the wrong preview");yield return Click("LookCompare");
            yield return CheckClassicWriteFailure();
            yield return Click("LookPresets");CheckStudioPanel("LookPopup");yield return RequireStudioPopupBlocksEditor();
            yield return Click("LookSlot_0");string firstPreset=JsonUtility.ToJson(lookDraft),priorProfile=JsonUtility.ToJson(profile);bool slotWasOccupied=HasLookSlot(0);
            if(!slotWasOccupied&&StudioButton("LookLoadSlot").interactable)throw new Exception("Empty preset slot can be loaded");
            yield return Click("LookSaveSlot");
            if(slotWasOccupied)
            {
                if(lookPopup!="overwrite"||JsonUtility.ToJson(profile)!=priorProfile)throw new Exception("Occupied preset was changed before confirmation");
                yield return Click("LookPopupConfirm");
            }
            if(lookPopup!="presets"||!HasLookSlot(0)||JsonUtility.ToJson(profile.lookSlots[0])!=firstPreset||JsonUtility.ToJson(RunLifecycle.ReadProfile(ProfilePath).lookSlots[0])!=firstPreset)throw new Exception("Preset save did not persist the current preview");
            Capture(Path.ChangeExtension(path,"presets.png"));
            string savedPreset=JsonUtility.ToJson(profile.lookSlots[0]),presetFile=File.ReadAllText(ProfilePath),chiefBeforeOverwrite=JsonUtility.ToJson(profile.chiefLook);
            yield return Click("LookPopupBack");yield return StudioSetOption(5,(lookDraft.parts[5]+1)%LookOptions(5).Length);string replacement=JsonUtility.ToJson(lookDraft);
            if(replacement==savedPreset)throw new Exception("Preset overwrite fixture must use a different preview");
            yield return Click("LookPresets");yield return Click("LookSaveSlot");if(lookPopup!="overwrite"||JsonUtility.ToJson(profile.lookSlots[0])!=savedPreset||File.ReadAllText(ProfilePath)!=presetFile)throw new Exception("Overwrite changed a preset before confirmation");
            CheckStudioPanel("LookPopup");yield return RequireStudioPopupBlocksEditor();Capture(Path.ChangeExtension(path,"preset-overwrite.png"));yield return Click("LookPopupKeep");
            if(lookPopup!=""||JsonUtility.ToJson(lookDraft)!=replacement||JsonUtility.ToJson(profile.lookSlots[0])!=savedPreset||File.ReadAllText(ProfilePath)!=presetFile)throw new Exception("Cancelling preset replacement modified its slot, disk or preview");
            yield return Click("LookPresets");yield return Click("LookSaveSlot");if(lookPopup!="overwrite")throw new Exception("Retrying preset replacement bypassed confirmation");yield return Click("LookPopupConfirm");
            if(lookPopup!="presets"||JsonUtility.ToJson(profile.lookSlots[0])!=replacement||JsonUtility.ToJson(RunLifecycle.ReadProfile(ProfilePath).lookSlots[0])!=replacement||JsonUtility.ToJson(profile.chiefLook)!=chiefBeforeOverwrite)throw new Exception("Confirmed preset replacement did not persist independently of chief appearance");
            yield return Click("LookPopupBack");
            yield return Click("LookRandom");yield return Click("LookPresets");yield return Click("LookLoadSlot");if(JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(profile.lookSlots[0]))throw new Exception("Preset roundtrip changed look");
            yield return Click("LookApply");yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");if(State.chiefLook.sex!=1)throw new Exception("Chief appearance not used");
            Capture(Path.ChangeExtension(path,"compact.png"));yield return Click("CommandOverview");
            if(modal=="campaignDossier")yield return Click("CampaignOrganization");
            if(modal!="commandOverview")throw new Exception("The campaign route did not open the organization overview");
            Capture(Path.ChangeExtension(path,"overview.png"));yield return Click("OverviewClose");
            yield return SmokeOpenBusiness(2);yield return Click("CommandPeopleDetails");Capture(Path.ChangeExtension(path,"member.png"));yield return Click("CommandEditLook");string original=JsonUtility.ToJson(State.agents.Find(a=>a.id==lookTarget).appearance);yield return Click("LookRandom");yield return Click("LookCancel");
            if(lookPopup!="discard")throw new Exception("Dirty preview closed without warning");CheckStudioPanel("LookPopup");yield return RequireStudioPopupBlocksEditor();Capture(Path.ChangeExtension(path,"discard.png"));
            string unsaved=JsonUtility.ToJson(lookDraft);yield return Click("LookPopupKeep");
            if(lookPopup!=""||JsonUtility.ToJson(lookDraft)!=unsaved||JsonUtility.ToJson(State.agents.Find(a=>a.id==lookTarget).appearance)!=original)throw new Exception("Keep editing lost draft or changed the actor");
            yield return Click("LookCancel");yield return Click("LookPopupConfirm");
            if(modal!="memberDetails"||JsonUtility.ToJson(State.agents.Find(a=>a.id==lookTarget).appearance)!=original)throw new Exception("Discard mutated actor or lost return location");
            yield return Click("MemberDetailsClose");yield return SmokeSaveLoad();
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_STUDIO_UI_SUCCESS: classic complete heads, two tabs, five male/four female facial rows, no legacy hair/ear controls; home/new-run/full-portrait captures, panel bounds/text, popup isolation, all-option cycles/wrap/Submit, independent parts, random-settings locks/manual edits, undo/redo, gender/beard, zoom, comparison, preset confirmation/persistence, chief apply, keep/discard and save/load");
        }
    }
}
