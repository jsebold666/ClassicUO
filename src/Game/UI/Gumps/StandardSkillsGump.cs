﻿using System;

using ClassicUO.Game.UI.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class StandardSkillsGump : Gump
    {

        private ExpandableScroll _scrollArea;
        private GumpPic _bottomLine, _bottomComment;
        private ScrollArea _container;
        private SkillControl[] _allSkillControls;
        private Label _skillsLabelSum;
        private Button _newGroupButton;

        public StandardSkillsGump() : base(0, 0)
        {
            CanBeSaved = true;
            AcceptMouseInput = false;
            CanMove = true;
            Height = 200;

            _scrollArea = new ExpandableScroll(0, 0, Height, 0x1F40, true)
            {
                TitleGumpID = 0x0834,
                AcceptMouseInput = true,
            };

            Add(_scrollArea);


            Label text = new Label("Show:   Real    Cap", false, 0x0386, 180, 1)
            {
                X = 30,
                Y = 33
            };
            Add(text);
            Add(new GumpPic(40, 60, 0x082B, 0));
            Add(_bottomLine = new GumpPic(40, Height - 98, 0x082B, 0));
            Add(_bottomComment = new GumpPic(40, Height - 85, 0x0836, 0));

            _container = new ScrollArea(25, 60 + _bottomLine.Height - 2, _scrollArea.Width - 14,
                _scrollArea.Height - 98, false) {AcceptMouseInput = true, CanMove = true};
            Add(_container);


            Add(_newGroupButton = new Button(0, 0x083A, 0x083A, 0x083A)
            {
                X = 60,
                Y = Height - 3,
                ContainsByBounds = true,
                ButtonAction = ButtonAction.Activate,
            });

            _allSkillControls = new SkillControl[FileManager.Skills.SkillsCount];

            foreach (KeyValuePair<string, List<int>> k in SkillsGroupManager.Groups)
            {
                AddSkillsToGroup(k.Key, k.Value);
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                string group = "New Group";
                if (SkillsGroupManager.AddNewGroup(group))
                    AddSkillsToGroup(group, SkillsGroupManager.GetSkillsInGroup(group));
            }
        }

        private void AddSkillsToGroup(string group, List<int> skills)
        {
            MultiSelectionShrinkbox box = new MultiSelectionShrinkbox(0, 0, _container.Width - 30, group, 0, 6, false, true)
            {
                CanMove = true,
                IsEditable = true
            };
            box.EditStateStart += (ss, e) =>
            {
                Control p = _container;
                var items = p.FindControls<ScrollAreaItem>().SelectMany(s => s.Children.OfType<MultiSelectionShrinkbox>());

                foreach (var item in items)
                {
                    foreach (EditableLabel c in item.FindControls<EditableLabel>())
                    {
                        c.SetEditable(false);
                    }
                }
            };

            box.EditStateEnd += (ss, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.BackupText) && !string.IsNullOrWhiteSpace(e.Text))
                {
                    SkillsGroupManager.ReplaceGroup(e.BackupText, e.Text);
                }
            };

            _container.Add(box);

            SkillControl[] controls = new SkillControl[skills.Count];
            int idx = 0;

            foreach (var skill in skills)
            {
                var c = new SkillControl(skill, box.Width - 15);
                c.Width = box.Width - 15;
                controls[idx++] = c;
                _allSkillControls[skill] = c;
            }
            box.SetItemsValue(controls);
        }


        public override void Update(double totalMS, double frameMS)
        {
            WantUpdateSize = true;

            _bottomLine.Y =  Height - 98;
            _bottomComment.Y = Height - 85;
            _container.Height = Height - 170;
            _newGroupButton.Y = Height - 55;

            _container.ForceUpdate();

            base.Update(totalMS, frameMS);
        }

        public void Update(int skillIndex)
        {
            if (skillIndex < _allSkillControls.Length)
                _allSkillControls[skillIndex]?.UpdateSkillValue();
        }


        protected override void OnDragBegin(int x, int y)
        {
            if (Engine.UI.MouseOverControl is SkillControl ctrl)
            {
                CanMove = false;
            }
            else
            {
                CanMove = true;
                base.OnDragBegin(x, y);
            }
        }

        //protected override void OnDragEnd(int x, int y)
        //{
        //    CanMove = true;
        //    base.OnDragEnd(x, y);
        //}

        class SkillControl : Control
        {
            private readonly Label _labelValue;
            private readonly int _skills;
            private bool _selected;

            public SkillControl(int skillIndex, int maxWidth)
            {
                AcceptMouseInput = true;
                CanMove = true;
                

                Skill skill = World.Player.Skills[skillIndex];
                _skills = skillIndex;
                if (skill.IsClickable)
                {
                    Button button = new Button(0, 0x0837, 0x0838, 0x0837);
                    button.MouseUp += (ss, e) => {  GameActions.UseSkill(skillIndex); };
                    Add(button);
                }
                
                Label label = new Label(skill.Name, false, 0x0288, maxwidth: maxWidth, font: 9)
                {
                    X = 12
                };
                Add(label);


                _labelValue = new Label(skill.Value.ToString("F1"), false, 0x0288, maxwidth: maxWidth - 10, font: 9, align: TEXT_ALIGN_TYPE.TS_RIGHT);
                Add(_labelValue);


                GumpPic @lock = new GumpPic(maxWidth - 8, 1, GetLockValue(skill.Lock), 0) {AcceptMouseInput = true};
                @lock.MouseUp += (sender, e) =>
                {
                    switch (skill.Lock)
                    {
                        case Lock.Up:
                            skill.Lock = Lock.Down;
                            GameActions.ChangeSkillLockStatus((ushort)skill.Index, (byte)Lock.Down);
                            @lock.Graphic = 0x985;
                            @lock.Texture = FileManager.Gumps.GetTexture(0x985);

                            break;

                        case Lock.Down:
                            skill.Lock = Lock.Locked;
                            GameActions.ChangeSkillLockStatus((ushort)skill.Index, (byte)Lock.Locked);
                            @lock.Graphic = 0x82C;
                            @lock.Texture = FileManager.Gumps.GetTexture(0x82C);

                            break;

                        case Lock.Locked:
                            skill.Lock = Lock.Up;
                            GameActions.ChangeSkillLockStatus((ushort)skill.Index, (byte)Lock.Up);
                            @lock.Graphic = 0x983;
                            @lock.Texture = FileManager.Gumps.GetTexture(0x983);

                            break;
                    }

                };
                Add(@lock);

                WantUpdateSize = false;

                Width = maxWidth;
                Height = label.Height;
            }


            private ushort GetLockValue(Lock lockStatus)
            {
                switch (lockStatus)
                {
                    case Lock.Up:

                        return 0x0984;
                    case Lock.Down:

                        return 0x0986;
                    case Lock.Locked:

                        return 0x082C;
                    default:

                        return Graphic.INVALID;
                }
            }

            private static Vector3 _hueVec = Vector3.Zero;

            protected override void OnMouseDown(int x, int y, MouseButton button)
            {
                _selected = true;
            }

            protected override void OnMouseUp(int x, int y, MouseButton button)
            {
                RootParent.CanMove = true;
                _selected = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (_selected)
                {
                    batcher.Draw2D(Textures.GetTexture(Color.Wheat), x, y, Width, Height, ref _hueVec);
                }

                return base.Draw(batcher, x, y);
            }


            public void UpdateSkillValue()
            {
                Skill skill = World.Player.Skills[_skills];
                if (skill != null)
                    _labelValue.Text = skill.Value.ToString("F1");
            }
        }
    }
}
