﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Structs.JobGauge;
using Dalamud.Plugin;
using ImGuiNET;

namespace DelvUIPlugin.Interface
{
    public class MachinistHudWindow : HudWindow
    {
        public override uint JobId => 31;

        protected int OverheatHeight => PluginConfiguration.MCHOverheatHeight;
        protected int OverheatWidth => PluginConfiguration.MCHOverheatWidth;
        protected new int XOffset => PluginConfiguration.MCHBaseXOffset;
        protected new int YOffset => PluginConfiguration.MCHBaseYOffset;
        protected int HeatGaugeHeight => PluginConfiguration.MCHHeatGaugeHeight;
        protected int HeatGaugeWidth => PluginConfiguration.MCHHeatGaugeWidth;
        protected int HeatGaugePadding => PluginConfiguration.MCHHeatGaugePadding;
        protected int HeatGaugeXOffset => PluginConfiguration.MCHHeatGaugeXOffset;
        protected int HeatGaugeYOffset => PluginConfiguration.MCHHeatGaugeYOffset;
        protected int BatteryGaugeHeight => PluginConfiguration.MCHBatteryGaugeHeight;
        protected int BatteryGaugeWidth => PluginConfiguration.MCHBatteryGaugeWidth;
        protected int BatteryGaugePadding => PluginConfiguration.MCHBatteryGaugePadding;
        protected int BatteryGaugeXOffset => PluginConfiguration.MCHBatteryGaugeXOffset;
        protected int BatteryGaugeYOffset => PluginConfiguration.MCHBatteryGaugeYOffset;
        protected bool WildfireEnabled => PluginConfiguration.MCHWildfireEnabled;
        protected int WildfireHeight => PluginConfiguration.MCHWildfireHeight;
        protected int WildfireWidth => PluginConfiguration.MCHWildfireWidth;
        protected int WildfireXOffset => PluginConfiguration.MCHWildfireXOffset;
        protected int WildfireYOffset => PluginConfiguration.MCHWildfireYOffset;
        protected Dictionary<string, uint> HeatColor => PluginConfiguration.JobColorMap[Jobs.MCH * 1000];
        protected Dictionary<string, uint> BatteryColor => PluginConfiguration.JobColorMap[Jobs.MCH * 1000 + 1];
        protected Dictionary<string, uint> RobotColor => PluginConfiguration.JobColorMap[Jobs.MCH * 1000 + 2];
        protected Dictionary<string, uint> OverheatColor => PluginConfiguration.JobColorMap[Jobs.MCH * 1000 + 3];
        protected Dictionary<string, uint> EmptyColor => PluginConfiguration.JobColorMap[Jobs.MCH * 1000 + 4];
        protected Dictionary<string, uint> WildfireColor => PluginConfiguration.JobColorMap[Jobs.MCH * 1000 + 5];
        private int InterBarOffset => PluginConfiguration.MCHInterBarOffset;
        // TODO: Rook auto-turret differences?
        private int[] RobotDuration = {12450, 13950, 15450, 16950, 18450, 19950};
        
        public MachinistHudWindow(DalamudPluginInterface pluginInterface, PluginConfiguration pluginConfiguration) : base(pluginInterface, pluginConfiguration) { }

        protected override void Draw(bool _) {
            DrawHealthBar();
            var nextHeight = DrawOverheatBar(0);
            nextHeight = DrawHeatGauge(nextHeight);
            nextHeight = DrawBatteryGauge(nextHeight);
            if (WildfireEnabled)
                nextHeight = DrawWildfireBar(nextHeight);
            DrawTargetBar();
        }

        private int DrawHeatGauge(int initialHeight)
        {
            var gauge = PluginInterface.ClientState.JobGauges.Get<MCHGauge>();
            
            var barWidth = (HeatGaugeWidth - HeatGaugePadding) / 2;
            var xPos = CenterX - XOffset + HeatGaugeXOffset;
            var yPos = CenterY + YOffset + initialHeight + HeatGaugeYOffset;
            var cursorPos = new Vector2(xPos + barWidth + HeatGaugePadding, yPos);
            const int chunkSize = 50;
            var barSize = new Vector2(barWidth, HeatGaugeHeight);
            
            var drawList = ImGui.GetWindowDrawList();
            
            for (var i = 2; i >= 1; i--)
            {
                var heat = Math.Max(Math.Min(gauge.Heat, chunkSize * i) - chunkSize * (i - 1), 0);
                var scale = (float) heat / chunkSize;
            
                drawList.AddRectFilled(cursorPos, cursorPos + barSize, 0x88000000);
                
                if (scale >= 1.0f)
                {
                    drawList.AddRectFilledMultiColor(
                        cursorPos, cursorPos + new Vector2(barWidth * scale, HeatGaugeHeight),
                        HeatColor["gradientLeft"], HeatColor["gradientRight"], HeatColor["gradientRight"], HeatColor["gradientLeft"]
                    );
                }
                else 
                {
                    drawList.AddRectFilledMultiColor(
                        cursorPos, cursorPos + new Vector2(barWidth * scale, HeatGaugeHeight), 
                        EmptyColor["gradientLeft"], EmptyColor["gradientRight"], EmptyColor["gradientRight"], EmptyColor["gradientLeft"]
                    );
                }
        
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);

                var heatText = heat.ToString();
                var textSize = ImGui.CalcTextSize(heatText);
                DrawOutlinedText(heatText, new Vector2(cursorPos.X + HeatGaugeWidth / 4f - textSize.X / 4f, cursorPos.Y-2));

                cursorPos = new Vector2(cursorPos.X - barWidth - HeatGaugePadding, cursorPos.Y);
            }

            return HeatGaugeHeight + initialHeight + InterBarOffset;
        }

        private int DrawBatteryGauge(int initialHeight)
        {
            var gauge = PluginInterface.ClientState.JobGauges.Get<MCHGauge>();
            var robotTimeLeft = gauge.RobotTimeRemaining;
            var robotPercentLeft = gauge.LastRobotBatteryPower != 0 ? (float) robotTimeLeft / RobotDuration[gauge.LastRobotBatteryPower / 10 - 5] : 0f;
            
            var barWidth = (BatteryGaugeWidth - BatteryGaugePadding * 9f) / 10;
            var xPos = CenterX - XOffset + BatteryGaugeXOffset;
            var yPos = CenterY + YOffset + initialHeight + BatteryGaugeYOffset;
            var cursorPos = new Vector2(xPos + barWidth * 9 + BatteryGaugePadding * 9, yPos);
            const int chunkSizeEnd = 10;
            const int chunkSizeStart = 50;
            var barSize = new Vector2(barWidth, BatteryGaugeHeight);
            var batteryGaugeHeight = gauge.IsRobotActive() ? BatteryGaugeHeight / 2 : BatteryGaugeHeight;
            
            var drawList = ImGui.GetWindowDrawList();

            int battery = 0;
            float scale = 0;
            
            for (var i = 10; i >= 6; i--)
            {
                battery = Math.Max(Math.Min(gauge.Battery, chunkSizeEnd * i) - chunkSizeEnd * (i - 1), 0);
                scale = (float) battery / chunkSizeEnd;
                
                drawList.AddRectFilled(cursorPos, cursorPos + barSize, 0x88000000);

                if (scale >= 1.0f)
                {
                    drawList.AddRectFilledMultiColor(
                        cursorPos, cursorPos + new Vector2(barWidth * scale, batteryGaugeHeight),
                        BatteryColor["gradientLeft"], BatteryColor["gradientRight"], BatteryColor["gradientRight"], BatteryColor["gradientLeft"]
                    );
                }
                else 
                {
                    drawList.AddRectFilledMultiColor(
                        cursorPos, cursorPos + new Vector2(barWidth * scale, batteryGaugeHeight), 
                        EmptyColor["gradientLeft"], EmptyColor["gradientRight"], EmptyColor["gradientRight"], EmptyColor["gradientLeft"]
                    );
                }

                if (gauge.IsRobotActive())
                {
                    var robotScale = Math.Min(Math.Max((robotPercentLeft - (i - 1) / 10f) * 10f, 0), 1);
                    drawList.AddRectFilledMultiColor(
                        cursorPos + new Vector2(0, batteryGaugeHeight), cursorPos + new Vector2(barWidth * robotScale, batteryGaugeHeight * 2), 
                        RobotColor["gradientLeft"], RobotColor["gradientRight"], RobotColor["gradientRight"], RobotColor["gradientLeft"]
                    );
                }
            
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);

                cursorPos = new Vector2(cursorPos.X - barWidth - BatteryGaugePadding, cursorPos.Y);
            }

            battery = Math.Min((int)gauge.Battery, chunkSizeStart);
            scale = (float) battery / chunkSizeStart;
            cursorPos = new Vector2(xPos, yPos);
            barWidth = (BatteryGaugeWidth - BatteryGaugePadding) / 2;
            barSize = new Vector2(barWidth, BatteryGaugeHeight);
            
            drawList.AddRectFilled(cursorPos, cursorPos + barSize, 0x88000000);

            if (scale >= 1.0f) 
            {
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(barWidth * scale, batteryGaugeHeight), 
                    BatteryColor["gradientLeft"], BatteryColor["gradientRight"], BatteryColor["gradientRight"], BatteryColor["gradientLeft"]
                );
            }
            else 
            {
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(barWidth * scale, batteryGaugeHeight), 
                    EmptyColor["gradientLeft"], EmptyColor["gradientRight"], EmptyColor["gradientRight"], EmptyColor["gradientLeft"]
                );
            }
            
            if (gauge.IsRobotActive())
            {
                var robotScale = Math.Max(Math.Min(robotPercentLeft, chunkSizeStart / 100f), 0);
                drawList.AddRectFilledMultiColor(
                    cursorPos + new Vector2(0, batteryGaugeHeight), cursorPos + new Vector2(barWidth * robotScale * 2, batteryGaugeHeight * 2), 
                    RobotColor["gradientLeft"], RobotColor["gradientRight"], RobotColor["gradientRight"], RobotColor["gradientLeft"]
                );
                
                var durationText = Math.Round(gauge.RobotTimeRemaining / 1000f).ToString();
                DrawOutlinedText(durationText, new Vector2(cursorPos.X + 5f, cursorPos.Y-2));
            }
            
            drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);

            return BatteryGaugeHeight + initialHeight + InterBarOffset;
        }

        private int DrawOverheatBar(int initialHeight)
        {
            var gauge = PluginInterface.ClientState.JobGauges.Get<MCHGauge>();
            var displayOverheat = gauge.IsOverheated();
            
            var barWidth = OverheatWidth;
            var xPos = CenterX - XOffset;
            var yPos = CenterY + YOffset + initialHeight;
            var cursorPos = new Vector2(xPos, yPos);
            var barSize = new Vector2(barWidth, OverheatHeight);
            
            var drawList = ImGui.GetWindowDrawList();
            
            drawList.AddRectFilled(cursorPos, cursorPos + barSize, 0x88000000);
            
            if (displayOverheat)
            {
                var duration = barWidth / 8000f * gauge.OverheatTimeRemaining;
                
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(duration, OverheatHeight),
                    OverheatColor["gradientLeft"], OverheatColor["gradientRight"], OverheatColor["gradientRight"], OverheatColor["gradientLeft"]
                );
                
                var durationText = Math.Round(gauge.OverheatTimeRemaining / 1000f).ToString();
                var textSize = ImGui.CalcTextSize(durationText);
                DrawOutlinedText(durationText, new Vector2(cursorPos.X + OverheatWidth / 2f - textSize.X / 2f, cursorPos.Y-2));
            }
            
            drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);

            return OverheatHeight + initialHeight + InterBarOffset;
        }

        private int DrawWildfireBar(int initialHeight)
        {
            var wildfireBuff = PluginInterface.ClientState.LocalPlayer.StatusEffects.Where(o => o.EffectId == 1946);
        
            var barWidth = WildfireWidth;
            var xPos = CenterX - XOffset + WildfireXOffset;
            var yPos = CenterY + YOffset + initialHeight + WildfireYOffset;
            var cursorPos = new Vector2(xPos, yPos);
            var barSize = new Vector2(barWidth, WildfireHeight);
        
            var drawList = ImGui.GetWindowDrawList();
        
            float duration = 0;
            drawList.AddRectFilled(cursorPos, cursorPos + barSize, 0x88000000);
            if (wildfireBuff.Any())
            {
                duration = Math.Abs(wildfireBuff.First().Duration);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(barSize.X / 10 * duration, barSize.Y),
                    WildfireColor["gradientLeft"], WildfireColor["gradientRight"], WildfireColor["gradientRight"], WildfireColor["gradientLeft"]
                );
            }
            
            drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
            
            var durationText = duration != 0 ? Math.Round(duration).ToString() : "";
            var textSize = ImGui.CalcTextSize(durationText);
            DrawOutlinedText(durationText, new Vector2(cursorPos.X + WildfireWidth / 2f - textSize.X / 2f, cursorPos.Y-2));

            return WildfireHeight + initialHeight + InterBarOffset;
        }
    }
}