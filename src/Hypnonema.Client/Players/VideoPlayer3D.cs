﻿namespace Hypnonema.Client.Players
{
    using System;
    using System.Threading.Tasks;

    using CitizenFX.Core.Native;

    using Hypnonema.Client.Dui;
    using Hypnonema.Client.Graphics;
    using Hypnonema.Client.Utils;
    using Hypnonema.Shared.Models;

    public class VideoPlayer3D : IVideoPlayer
    {
        private readonly ScaleformRenderer scaleform;

        private VideoPlayer3D(Screen screen, ScaleformRenderer scaleform, DuiBrowser duiBrowser)
        {
            this.ScreenName = screen.Name;

            this.GlobalVolume = screen.BrowserSettings.GlobalVolume;
            this.SoundMaxDistance = screen.BrowserSettings.SoundMaxDistance;
            this.SoundMinDistance = screen.BrowserSettings.SoundMinDistance;
            this.SoundAttenuation = screen.BrowserSettings.SoundAttenuation;

            this.DuiBrowser = duiBrowser;

            this.scaleform = scaleform;
        }

        ~VideoPlayer3D()
        {
            this.Dispose();
        }

        public DuiBrowser DuiBrowser { get; }

        public float GlobalVolume { get; set; }

        public bool IsOutOfRange => this.scaleform.GetDistanceToPlayer() > this.MaxRenderDistance;

        public float MaxRenderDistance { get; set; } = ConfigReader.GetConfigKeyValue(
            API.GetCurrentResourceName(),
            "hypnonema_max_render_distance",
            0,
            400f);

        public string ScreenName { get; }

        public float SoundAttenuation { get; set; }

        public float SoundMaxDistance { get; set; }

        public float SoundMinDistance { get; set; }

        public static async Task<IVideoPlayer> CreateVideoPlayer(Screen screen, DuiBrowser duiBrowser)
        {
            var scaleform = await ScaleformRendererPool.Instance.AcquireScaleformRenderer(
                                screen.PositionalSettings,
                                duiBrowser.TxdName,
                                duiBrowser.TxnName);

            if (scaleform != null) return new VideoPlayer3D(screen, scaleform, duiBrowser);

            Logger.Error("Cannot create VideoPlayer3D, because of failed Scaleform creation.");
            return null;
        }

        public void CalculateVolume()
        {
            var distance = this.scaleform.GetDistanceToPlayer();

            if (distance >= this.SoundMaxDistance || this.IsOutOfRange)
            {
                this.DuiBrowser.SetVolume(0f);
                return;
            }

            this.DuiBrowser.SetVolume(this.GetSoundFactor(distance) * this.GlobalVolume);
        }

        public void Dispose()
        {
            if (this.DuiBrowser.IsValid) DuiBrowserPool.Instance.ReleaseDuiBrowser(this.DuiBrowser);

            // TODO: Dispose Scaleform
            ScaleformRendererPool.Instance.ReleaseScaleformRenderer(this.scaleform);

            GC.SuppressFinalize(this);
        }

        public void Draw()
        {
            this.scaleform.Draw();
        }

        public void OnTick()
        {
            if (this.IsOutOfRange)
            {
                this.DuiBrowser.ShowPlayer(false);
                return;
            }

            this.DuiBrowser.ShowPlayer(true);

            this.CalculateVolume();
            this.Draw();
        }

        public void Pause()
        {
            this.DuiBrowser.Pause();
        }

        public void Play(string url)
        {
            this.DuiBrowser.Play(url);
        }

        public void Resume()
        {
            this.DuiBrowser.Resume();
        }

        public void Seek(float time)
        {
            this.DuiBrowser.Seek(time);
        }

        public void Stop()
        {
            this.DuiBrowser.Stop();
        }

        public void SynchronizeState(bool paused, float currentTime, string currentSource, bool repeat)
        {
            this.DuiBrowser.SynchronizeState(paused, currentTime, currentSource, repeat);
        }

        public void ToggleRepeat()
        {
            this.DuiBrowser.ToggleRepeat();
        }

        private float GetSoundFactor(float distance)
        {
            return this.SoundMinDistance / (this.SoundMinDistance + this.SoundAttenuation
                                            * (Math.Max(distance, this.SoundMinDistance) - this.SoundMinDistance));
        }
    }
}