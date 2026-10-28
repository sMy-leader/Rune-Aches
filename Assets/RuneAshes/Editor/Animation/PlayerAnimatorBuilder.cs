// ─────────────────────────────────────────────────────────────────────────────
// 【职责】生成玩家专用的 Animator Controller 与上半身 AvatarMask。
// 【为什么不用素材自带的 controller】它没有参数、AnyState 无条件转 Death/Hit，
//        一进 Play 就会卡死；这里按 Docs/代码导读 的玩家状态机表重建。
// 【可重复运行】菜单 Rune Ashes/Animation/Build Player Animator 每次都会覆盖生成物，
//        不会改原始 FBX 或素材自带的 controller。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RuneAshes.EditorTools
{
    public static class PlayerAnimatorBuilder
    {
        private const string FbxPath = "Assets/Art/Incoming/Player/Meshes/Hu_F_Magician.fbx";
        private const string ControllerPath = "Assets/Art/Incoming/Player/Hu_F_Magician_Player.controller";
        private const string MaskPath = "Assets/Art/Incoming/Player/Hu_F_Magician_UpperMask.mask";

        private static readonly string[] RequiredClips =
        {
            "idle",
            "Walk_Forward", "Run_Forward",
            "Walk_Backwards", "Run_Backwards",
            "Walk_Left", "Run_Left",
            "Walk_Right", "Run_Right",
            "Combat_Cast_Attack", "Combat_2HL_Hit", "Death"
        };

        [MenuItem("Rune Ashes/Animation/Build Player Animator")]
        public static void Build()
        {
            var clips = LoadClips();
            if (clips == null)
            {
                return;
            }

            var missing = RequiredClips.Where(name => !clips.ContainsKey(name)).ToArray();
            if (missing.Length > 0)
            {
                Debug.LogError("[Animator] 缺少动画 clip: " + string.Join(", ", missing) +
                               "。请检查 FBX 的动画导入设置。");
                return;
            }

            var mask = BuildUpperBodyMask();
            if (mask == null)
            {
                return;
            }

            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Cast", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            BuildBaseLayer(controller, clips);
            BuildCastLayer(controller, clips, mask);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Animator] 已生成 " + ControllerPath + " 与 " + MaskPath);
        }

        private static Dictionary<string, AnimationClip> LoadClips()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[Animator] 找不到 " + FbxPath);
                return null;
            }

            return assets
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .GroupBy(clip => clip.name)
                .ToDictionary(group => group.Key, group => group.First());
        }

        private static void BuildBaseLayer(AnimatorController controller, Dictionary<string, AnimationClip> clips)
        {
            var layers = controller.layers;
            layers[0].name = "Base Layer";
            layers[0].defaultWeight = 1f;
            controller.layers = layers;

            var stateMachine = layers[0].stateMachine;
            stateMachine.name = "Base Layer";

            var locomotion = stateMachine.AddState("Locomotion");
            locomotion.motion = CreateLocomotionTree(controller, clips);
            stateMachine.defaultState = locomotion;

            var hit = stateMachine.AddState("Hit");
            hit.motion = clips["Combat_2HL_Hit"];

            var death = stateMachine.AddState("Death");
            death.motion = clips["Death"];

            // 顺序即优先级：Death 先加，保证死亡不会被受击打断。
            var toDeath = stateMachine.AddAnyStateTransition(death);
            toDeath.hasExitTime = false;
            toDeath.duration = 0.1f;
            toDeath.canTransitionToSelf = false;
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

            var toHit = stateMachine.AddAnyStateTransition(hit);
            toHit.hasExitTime = false;
            toHit.duration = 0.05f;
            toHit.canTransitionToSelf = false;
            toHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");

            var hitExit = hit.AddTransition(locomotion);
            hitExit.hasExitTime = true;
            hitExit.exitTime = 0.9f;
            hitExit.duration = 0.15f;
        }

        private static BlendTree CreateLocomotionTree(
            AnimatorController controller,
            Dictionary<string, AnimationClip> clips)
        {
            var tree = new BlendTree
            {
                name = "Locomotion Blend Tree",
                blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY"
            };

            AssetDatabase.AddObjectToAsset(tree, controller);

            // 输入幅值 0~1：0.45 走路、1.0 跑步；方向由 X/Y 决定。
            tree.AddChild(clips["idle"], new Vector2(0f, 0f));

            tree.AddChild(clips["Walk_Forward"], new Vector2(0f, 0.45f));
            tree.AddChild(clips["Run_Forward"], new Vector2(0f, 1f));

            tree.AddChild(clips["Walk_Backwards"], new Vector2(0f, -0.45f));
            tree.AddChild(clips["Run_Backwards"], new Vector2(0f, -1f));

            tree.AddChild(clips["Walk_Left"], new Vector2(-0.45f, 0f));
            tree.AddChild(clips["Run_Left"], new Vector2(-1f, 0f));

            tree.AddChild(clips["Walk_Right"], new Vector2(0.45f, 0f));
            tree.AddChild(clips["Run_Right"], new Vector2(1f, 0f));

            return tree;
        }

        private static void BuildCastLayer(
            AnimatorController controller,
            Dictionary<string, AnimationClip> clips,
            AvatarMask mask)
        {
            controller.AddLayer("Cast (Upper Body)");

            var layers = controller.layers;
            var index = Array.FindIndex(layers, layer => layer.name == "Cast (Upper Body)");
            layers[index].defaultWeight = 1f;
            layers[index].avatarMask = mask;
            controller.layers = layers;

            var stateMachine = layers[index].stateMachine;
            stateMachine.name = "Cast (Upper Body)";

            // 空状态：不覆盖任何骨骼，让 Base Layer 的下半身继续跑。
            var empty = stateMachine.AddState("Empty");
            empty.motion = null;
            stateMachine.defaultState = empty;

            var cast = stateMachine.AddState("Cast");
            cast.motion = clips["Combat_Cast_Attack"];

            var toCast = stateMachine.AddAnyStateTransition(cast);
            toCast.hasExitTime = false;
            toCast.duration = 0.1f;
            toCast.canTransitionToSelf = false;
            toCast.AddCondition(AnimatorConditionMode.If, 0f, "Cast");

            var castExit = cast.AddTransition(empty);
            castExit.hasExitTime = true;
            castExit.exitTime = 0.9f;
            castExit.duration = 0.1f;
        }

        private static AvatarMask BuildUpperBodyMask()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (model == null)
            {
                Debug.LogError("[Animator] 找不到模型 " + FbxPath);
                return null;
            }

            var skinned = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinned == null || skinned.bones == null)
            {
                Debug.LogError("[Animator] 模型没有 SkinnedMeshRenderer.bones，无法生成上半身 Mask。");
                return null;
            }

            var paths = new List<string>();
            foreach (var bone in skinned.bones)
            {
                if (bone == null)
                {
                    continue;
                }

                var name = bone.name.ToLowerInvariant();

                // 排除下半身；其余上半身骨骼全部激活。
                if (name.Contains("thigh") || name.Contains("calf") ||
                    name.Contains("foot") || name.Contains("toe"))
                {
                    continue;
                }

                var upper = name.Contains("pelvis") || name.Contains("spine") ||
                            name.Contains("neck") || name.Contains("head") ||
                            name.Contains("clavicle") || name.Contains("upperarm") ||
                            name.Contains("lowerarm") || name.Contains("hand") ||
                            name.Contains("finger") || name.Contains("thumb") ||
                            name.Contains("index") || name.Contains("middle") ||
                            name.Contains("ring") || name.Contains("pinky");

                if (upper)
                {
                    paths.Add(GetPath(bone, model.transform));
                }
            }

            if (paths.Count == 0)
            {
                Debug.LogError("[Animator] 没有匹配到上半身骨骼，请检查骨骼命名。");
                return null;
            }

            AssetDatabase.DeleteAsset(MaskPath);
            var mask = new AvatarMask();
            mask.transformCount = paths.Count;
            for (var i = 0; i < paths.Count; i++)
            {
                mask.SetTransformPath(i, paths[i]);
                mask.SetTransformActive(i, true);
            }

            AssetDatabase.CreateAsset(mask, MaskPath);
            return mask;
        }

        private static string GetPath(Transform bone, Transform root)
        {
            var path = bone.name;
            var parent = bone.parent;
            while (parent != null && parent != root)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
