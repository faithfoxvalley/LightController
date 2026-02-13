using LightController.Config.Bacnet;
using LightController.Midi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LightController.Bacnet;

public partial class BacnetProcessor
{
    private readonly BacnetClient bacnetClient;
    private readonly ConcurrentDictionary<uint, BacNode> nodes = new ConcurrentDictionary<uint, BacNode>();
    private readonly Dictionary<string, BacnetEvent> namedEvents = new Dictionary<string, BacnetEvent>();
    private readonly Dictionary<MidiNote, BacnetEvent> midiEvents = new Dictionary<MidiNote, BacnetEvent>();
    private readonly List<BacnetEvent> allEvents = new List<BacnetEvent>();

    private readonly object writeRequestsLock = new object();
    private readonly Dictionary<BacnetEndpoint, BacnetRequest> writeRequests = new Dictionary<BacnetEndpoint, BacnetRequest>();
    private readonly ListBox bacnetList;

    // used to send whois packets every 10ish seconds
    private int WhoIsTick = 0; 
    private const int WhoIsInterval = 100;


    public bool Enabled { get; }

    public BacnetProcessor(BacnetConfig config, System.Windows.Controls.ListBox bacnetList)
    {
        if (config?.Events == null || config.Events.Count == 0)
            return;

        foreach(BacnetEvent e in config.Events)
        {
            e.Init();
            if(!string.IsNullOrWhiteSpace(e.Name))
                namedEvents[e.Name] = e;
            if (e.MidiNote != null)
                midiEvents[e.MidiNote] = e;
            bacnetList.Items.Add(e.ToString());
            allEvents.Add(e);
        }

        bacnetList.SelectionChanged += ListboxChanged;

        ushort port = 0xBAC0;
        if(config.Port > 0)
            port = config.Port;

        BacnetIpUdpProtocolTransport transport;
        if (string.IsNullOrWhiteSpace(config.BindIp) || !IPAddress.TryParse(config.BindIp, out _))
        {
            transport = new BacnetIpUdpProtocolTransport(port);
            Log.Info($"[Bacnet] Starting Bacnet client at {IPAddress.Any}:{port}");
        }
        else
        {
            transport = new BacnetIpUdpProtocolTransport(port, localEndpointIp: config.BindIp);
            Log.Info($"[Bacnet] Starting Bacnet client at {config.BindIp}:{port}");
        }
        bacnetClient = new BacnetClient(transport);
        bacnetClient.Start();
        bacnetClient.OnIam += OnIamReceived;
        bacnetClient.OnError += BacnetClient_OnError;
        bacnetClient.Retries = 1;

        bacnetClient.WhoIs();

        Enabled = true;
        this.bacnetList = bacnetList;
    }

    private void BacnetClient_OnError(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetErrorClasses errorClass, BacnetErrorCodes errorCode, byte[] buffer, int offset, int length)
    {
        Log.Error($"[Bacnet] Pdu:{type} Service:{service} Error:{errorClass} {errorCode}");
    }

    private void ListboxChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        int prevSelection = -1;
        if (e.RemovedItems.Count > 0)
            prevSelection = bacnetList.Items.IndexOf(e.RemovedItems[0]);

        int currSelection = -1;
        if (e.AddedItems.Count > 0)
            currSelection = bacnetList.Items.IndexOf(e.AddedItems[0]);
        if (currSelection < 0 || currSelection >= bacnetList.Items.Count || currSelection == prevSelection)
            return;

        QueueEvent(allEvents[currSelection]);
        if (prevSelection < bacnetList.Items.Count)
            SetListSelection(prevSelection);
    }
    private void SetListSelection(int index)
    {
        bacnetList.SelectionChanged -= ListboxChanged;
        bacnetList.SelectedIndex = index;
        bacnetList.SelectionChanged += ListboxChanged;
    }


    public void TriggerEvents(MidiNote note)
    {
        if (!Enabled)
            return;

        if (note == null || !midiEvents.TryGetValue(note, out BacnetEvent e))
            return;

        QueueEvent(e);
    }

    public void TriggerEvents(MidiNote note, IEnumerable<string> names)
    {
        if (!Enabled)
            return;
        
        List<BacnetEvent> events = new List<BacnetEvent>();
        if (note != null && midiEvents.TryGetValue(note, out BacnetEvent midiEvent))
            events.Add(midiEvent);

        if (names != null)
        {
            foreach (string name in names)
            {
                if (name != null && namedEvents.TryGetValue(name, out BacnetEvent namedEvent))
                    events.Add(namedEvent);
            }
        }

        if (events.Count == 0)
            return;


        QueueEvents(events);
    }

    private void QueueEvents(IEnumerable<BacnetEvent> events)
    {
        foreach (BacnetEvent e in events)
            Log.Info("[Bacnet] Event: " + e);
        lock (writeRequestsLock)
        {
            foreach (BacnetEvent e in events)
            {
                foreach (BacnetProperty prop in e.Properties)
                    writeRequests[prop.Endpoint] = prop.ValueRequest;
            }
        }
    }

    private void QueueEvent(BacnetEvent e)
    {
        Log.Info("[Bacnet] Event: " + e);
        lock (writeRequestsLock)
        {
            foreach (BacnetProperty prop in e.Properties)
                writeRequests[prop.Endpoint] = prop.ValueRequest;
        }
    }

    public void TriggerEvents(IEnumerable<string> names)
    {
        if (!Enabled)
            return;
        
        if (names == null)
            return;

        List<BacnetEvent> events = new List<BacnetEvent>();
        foreach (string name in names)
        {
            if (name != null && namedEvents.TryGetValue(name, out BacnetEvent namedEvent))
                events.Add(namedEvent);
        }

        if (events.Count == 0)
            return;

        QueueEvents(events);
    }

    private void OnIamReceived(BacnetClient sender, BacnetAddress adr, uint deviceId, uint maxApdu, BacnetSegmentations segmentation, ushort vendorId)
    {
        Log.Info($"[Bacnet] Found Bacnet device: {adr} - {deviceId}");
        nodes.TryAdd(deviceId, new BacNode(adr, deviceId));
    }

    public bool WriteValue(uint deviceId, BacnetObjectId objectId, BacnetValue value)
    {
        if (nodes.TryGetValue(deviceId, out BacNode node))
        {
            if (!bacnetClient.WritePropertyRequest(node.Address, objectId, BacnetPropertyIds.PROP_PRESENT_VALUE, new[] { value }))
                Log.Warn($"[Bacnet] Unable to verify property write succeeded");
            return true;
        }
        return false;
    }


    public bool ReadValue(uint deviceId, BacnetObjectId objectId, out IList<BacnetValue> value)
    {
        if(nodes.TryGetValue(deviceId, out BacNode node))
            return bacnetClient.ReadPropertyRequest(node.Address, objectId, BacnetPropertyIds.PROP_PRESENT_VALUE, out value);
        value = new List<BacnetValue>();
        return false;
    }

    public void Update()
    {
        List<BacnetRequest> writeRequests;
        lock (writeRequestsLock)
        {
            writeRequests = this.writeRequests.Values.ToList();
            this.writeRequests.Clear();
        }

        bool sendWhoIs = false;
        for (int i = writeRequests.Count - 1; i >= 0; i--)
        {
            BacnetRequest request = writeRequests[i];
            if (WriteValue(request.Endpoint.DeviceId, request.Endpoint.ObjectId, request.Value))
            {
                int lastIndex = writeRequests.Count - 1;
                if (i < lastIndex)
                    writeRequests[i] = writeRequests[lastIndex];
                writeRequests.RemoveAt(lastIndex);
            }
            else
            {
                sendWhoIs = true;
            }
        }

        if (sendWhoIs)
        {
            if (WhoIsTick % WhoIsInterval == 0)
            {
                Log.Info("[Bacnet] Failed to contact one or more Bacnet devices. Sending Bacnet WhoIs request");
                bacnetClient.WhoIs();
            }
            WhoIsTick++;
        }
        else
        {
            WhoIsTick = 0;
        }

        if (writeRequests.Count <= 0)
            return;

        lock (writeRequestsLock)
        {
            foreach (BacnetRequest request in writeRequests)
                this.writeRequests.TryAdd(request.Endpoint, request);
        }
    }

}
