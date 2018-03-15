﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEditor;
using System;
using Ntreev.Library.Psd;
using PSDUnity.Data;
using PSDUnity.UGUI;
using UnityEditor.IMGUI.Controls;

namespace PSDUnity.Analysis
{
    [CustomEditor(typeof(Exporter))]
    public class ExporterDrawer : Editor
    {
        private SerializedProperty scriptProp;
        private Exporter exporter;
        private const string Prefs_LastPsdsDir = "lastPsdFileDir";
        private ExporterTreeView m_TreeView;
        private GroupNode rootNode;
        [SerializeField] TreeViewState m_TreeViewState = new TreeViewState();
        private void OnEnable()
        {
            exporter = target as Exporter;
            scriptProp = serializedObject.FindProperty("m_Script");
            AutoChargeRule();
            InitTreeView();
        }

        private void AutoChargeRule()
        {
            if (exporter.ruleObj == null)
            {
                exporter.ruleObj = PsdResourceUtil.DefultRuleObj();
            }

            if (exporter.settingObj == null)
            {
                exporter.settingObj = PsdResourceUtil.DefultSettingObj();
            }
        }

        private void InitTreeView()
        {
            if (exporter.groups != null && exporter.groups.Count > 0)
            {
                if (exporter.groups.Count > 0)
                {
                    rootNode = TreeViewUtility.ListToTree<GroupNode>(exporter.groups);
                    m_TreeView = new ExporterTreeView(m_TreeViewState);
                    m_TreeView.root = rootNode;
                }
            }
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(exporter, typeof(Exporter), false);
            EditorGUILayout.PropertyField(scriptProp);
            EditorGUI.EndDisabledGroup();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPathOption();
            DrawConfigs();
            if (m_TreeView != null && rootNode != null)
            {
                DrawUICreateOption();
                DrawGroupNode();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawUICreateOption()
        {
            using (var hor = new EditorGUILayout.HorizontalScope())
            {
                var style = "miniButton";
                var layout = GUILayout.Width(60);
                if (GUILayout.Button("Build-All", style, layout))
                {
                    var canvasObj = Array.Find(Selection.objects, x => x is GameObject && (x as GameObject).GetComponent<Canvas>() != null);
                    PSDImporter.InitEnviroment(exporter.ruleObj, exporter.settingObj.defultUISize, canvasObj == null ? FindObjectOfType<Canvas>() : (canvasObj as GameObject).GetComponent<Canvas>());
                    PSDImporter.StartBuild(rootNode);
                    AssetDatabase.Refresh();
                }

                if (GUILayout.Button("Build-Sel", style, layout))
                {
                    var canvasObj = Array.Find(Selection.objects, x => x is GameObject && (x as GameObject).GetComponent<Canvas>() != null);
                    PSDImporter.InitEnviroment(exporter.ruleObj, exporter.settingObj.defultUISize, canvasObj == null ? FindObjectOfType<Canvas>() : (canvasObj as GameObject).GetComponent<Canvas>());
                    foreach (var node in m_TreeView.selected){
                        PSDImporter.StartBuild(node);
                    }
                    AssetDatabase.Refresh();
                }

                if (GUILayout.Button("Expland", style, layout))
                {
                    m_TreeView.ExpandAll();
                }

            }

        }

        private void DrawGroupNode()
        {
            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, m_TreeView.totalHeight);
            m_TreeView.OnGUI(rect);
        }

        private void RecordAllPsdInformation()
        {
            if (!string.IsNullOrEmpty(exporter.psdFile))
            {
                var psd = PsdDocument.Create(exporter.psdFile);
                if (psd != null)
                {
                    ExportUtility.InitPsdExportEnvrioment(exporter, new Vector2(psd.Width, psd.Height));
                    rootNode = new GroupNode(new Rect(Vector2.zero, exporter.settingObj.defultUISize), 0, -1);
                    rootNode.displayName =  exporter.name;
                    var groupDatas = ExportUtility.CreatePictures(psd.Childs, new Vector2(psd.Width, psd.Height), exporter.settingObj.defultUISize, exporter.settingObj.forceSprite);
                    if (groupDatas != null)
                    {
                        foreach (var groupData in groupDatas)
                        {
                            rootNode.AddChild(groupData);
                            ExportUtility.ChargeTextures(exporter, groupData);
                        }
                    }
                    TreeViewUtility.TreeToList<GroupNode>(rootNode, exporter.groups, true);
                    EditorUtility.SetDirty(exporter);
                }
            }
        }

        private void DrawPathOption()
        {
            using (var hor = new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("文档路径:", GUILayout.Width(60));
                if (GUILayout.Button(new GUIContent(exporter.psdFile, "点击此处选择文件夹！"), EditorStyles.textField))
                {
                    var dir = PlayerPrefs.GetString(Prefs_LastPsdsDir);
                    if (string.IsNullOrEmpty(dir))
                    {
                        if (!string.IsNullOrEmpty(exporter.psdFile))
                        {
                            dir = System.IO.Path.GetDirectoryName(exporter.psdFile);
                        }
                    }

                    if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir))
                    {
                        dir = Application.dataPath;
                    }

                    var path = EditorUtility.OpenFilePanel("选择一个pdf文件", dir, "psd");

                    if (!string.IsNullOrEmpty(path))
                    {
                        exporter.psdFile = path;
                        PlayerPrefs.SetString(Prefs_LastPsdsDir, System.IO.Path.GetDirectoryName(path));
                    }
                }
            }
        }

        private void DrawConfigs()
        {
            using (var hor = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("用户定义: ");

                if (GUILayout.Button("[规则]", EditorStyles.label))
                {
                    if (EditorUtility.DisplayDialog("创建新规则", "确认后将生成新的规则文件！", "确认", "取消"))
                    {
                        exporter.ruleObj = ScriptableObject.CreateInstance<RuleObject>();
                        ProjectWindowUtil.CreateAsset(exporter.ruleObj, "new rule.asset");
                    }
                }
                exporter.ruleObj = EditorGUILayout.ObjectField(exporter.ruleObj, typeof(RuleObject), false) as RuleObject;


                if (GUILayout.Button("[设置]", EditorStyles.label))
                {
                    if (EditorUtility.DisplayDialog("创建新设置", "确认后将生成新的设置文件！", "确认", "取消"))
                    {
                        exporter.settingObj = ScriptableObject.CreateInstance<SettingObject>();
                        ProjectWindowUtil.CreateAsset(exporter.settingObj, "new setting.asset");
                    }
                }
                exporter.settingObj = EditorGUILayout.ObjectField(exporter.settingObj, typeof(SettingObject), false) as SettingObject;
            }

            if (GUILayout.Button("转换层级为图片，并记录索引", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("温馨提示","重新加载目前将重写以下配制，继续请按确认！","确认"))
                {
                    RecordAllPsdInformation();
                    m_TreeView = new ExporterTreeView(m_TreeViewState);
                    m_TreeView.root = rootNode;
                }
            
            }
        }

    }

}