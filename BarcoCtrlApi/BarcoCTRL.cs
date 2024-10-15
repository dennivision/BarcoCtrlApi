using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Independentsoft.Exchange;

namespace BarcoCtrlApi
{
    public class BarcoCTRL
    {
        public int Debug;
        private OAuth2Client _oauthclient;
        private BarcoCtrlApiClient _apiClient;
        private List<SourceDto> _sources;
        private List<WorkplaceDto> _workplaces;
        private List<VisualObjectDto> _workplaceContent;

        public delegate void SourceUpdateHandler(ushort idx,
            SimplSharpString id,
            SimplSharpString name,
            SimplSharpString type,
            SimplSharpString sclass,
            ushort hasAudio,
            ushort isInteractive);
        
        public SourceUpdateHandler UpdateSource { get; set; }

        public delegate void WorkplaceUpdateHandler(ushort idx,
            SimplSharpString id,
            SimplSharpString name,
            SimplSharpString type,
            ushort width,
            ushort height,
            ushort columns,
            ushort rows);
        
        public WorkplaceUpdateHandler UpdateWorkplace { get; set; }
        
        public ushort Initialize(SimplSharpString hostname,
            SimplSharpString clientId,
            SimplSharpString clientSecret)
        {
            try
            {
                var tokenUrl = $"https://{hostname}/auth/realms/OCS/protocol/openid-connect/token";

                CrestronConsole.PrintLine(tokenUrl);

                _oauthclient = new OAuth2Client(clientId.ToString(), clientSecret.ToString(), tokenUrl);

                CrestronConsole.PrintLine($"clientId: {clientId}");
                CrestronConsole.PrintLine($"clientSecret: {clientSecret}");
                CrestronConsole.PrintLine($"tokenUrl: {tokenUrl}");
                var baseUrl = $"https://{hostname}/api/operate/v3";

                _apiClient = new BarcoCtrlApiClient(baseUrl, _oauthclient);

                CrestronConsole.PrintLine(baseUrl);

                _sources = _apiClient.GetSourcesAsync().GetAwaiter().GetResult();

                _workplaces = _apiClient.GetWorkplacesAsync().GetAwaiter().GetResult();

                return (0);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Exception With initialize(): {e.Message}");
                return (65535);
            }
        }
        
        public ushort GetSourcesCount()
        {
            return (ushort)(_sources?.Count ?? 0);
        }

        public ushort GetWorkplacesCount()
        {
            return (ushort)(_workplaces?.Count ?? 0);
        }

        public void GetSourceInfo(ushort idx)
        {
            if (idx >= _sources.Count)
            {
                CrestronConsole.PrintLine($"Index {idx} is out of range.");
                return;
            }

            var source = _sources[idx];
            var hasAudio = (ushort)(source.Audio != null ? 1 : 0);
            var isInteractive = (ushort)(source.Interactivity != null ? 1 : 0);

            UpdateSource?.Invoke(idx, new SimplSharpString(source.Id), new SimplSharpString(source.Name),
                new SimplSharpString(source.Type), new SimplSharpString(source.Class), hasAudio, isInteractive);
        }

        public void GetWorkplaceInfo(ushort idx)
        {
            try
            {
                if (idx >= _workplaces.Count)
                {
                    CrestronConsole.PrintLine($"Index {idx} is out of range.");
                    return;
                }

                var workplace = _workplaces[idx];

                var width = (ushort)(workplace.WallGeometry != null ? workplace.WallGeometry.SizePx.Width : 0);
                var height = (ushort)(workplace.WallGeometry != null ? workplace.WallGeometry.SizePx.Height : 0);
                var columns = (ushort)(workplace.WallGeometry != null ? workplace.WallGeometry.Grid.Columns : 0);
                var rows = (ushort)(workplace.WallGeometry != null ? workplace.WallGeometry.Grid.Rows : 0);

                UpdateWorkplace?.Invoke(idx, new SimplSharpString(workplace.Id), new SimplSharpString(workplace.Name),
                    new SimplSharpString(workplace.Type), width, height, columns, rows);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Exception With GetWorkplaceInfo({idx}): {e.Message}");
            }
        }

        public ushort GetWorkplaceContent(ushort idx)
        {
            if (idx >= _workplaces.Count)
            {
                CrestronConsole.PrintLine("Invalid workplace index.");
                _workplaceContent = new List<VisualObjectDto>();
                return 0;
            }

            var workplace = _workplaces[idx];
            _workplaceContent = _apiClient.GetWorkplaceContentAsync(workplace.Id).GetAwaiter().GetResult();
            CrestronConsole.PrintLine(
                $"GetWorkPlaceContent({idx}) -  workplace.Name: {workplace.Name}, _workplaceContent.Count: {_workplaceContent.Count}");
            foreach (var visualObject in _workplaceContent)
                CrestronConsole.PrintLine(
                    $"Visual Object ID: {visualObject.Id}, Window.Title: {visualObject.Window.Title}, Content.id: {visualObject.Content.Id}");
            return (ushort)_workplaceContent.Count;
        }

        public string GetWindowTitle(ushort idx)
        {
            if (idx >= _workplaceContent.Count) return string.Empty;

            var visualObject = _workplaceContent[idx];
            return visualObject.Window.Title;
        }
        
        public ushort GetWindowSourceIndex(ushort idx)
        {
            if (idx >= _workplaceContent.Count) return (ushort)65535;

            var visualObject = _workplaceContent[idx];
            var source = _sources.FirstOrDefault(s => s.Id == visualObject.Content.Id);

            return source != null ? (ushort)_sources.IndexOf(source) : (ushort)65535;
        }
        
        public void SetWindowSourceIndex(ushort window, ushort srcIdx, ushort wpIdx)
        {
            try
            {
                CrestronConsole.PrintLine($"SetWindowSourceIndex({window}, {srcIdx}, {wpIdx})");
                if (wpIdx >= _workplaces.Count || srcIdx >= _sources.Count || window >= _workplaceContent.Count) return;

                var workplace = _workplaces[wpIdx];
                var source = _sources[srcIdx];

                _workplaceContent[window].Content.Id = source.Id;

                _apiClient.PutWorkplaceContentAsync(workplace.Id, _workplaceContent).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Exception With SetWindowSourceIndex({window}, {srcIdx}, {wpIdx}): {e.Message}");
            }
        }
    }
}
