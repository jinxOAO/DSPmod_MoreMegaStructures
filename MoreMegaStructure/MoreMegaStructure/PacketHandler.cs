using NebulaAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreMegaStructure
{
    public class MegastructurePacket
    {
        // Only use auto properties here
        public int planetId { get; set; }

        // Save any primitive type here
        public int customValue { get; set; }

        // If you have data structures that are not primitives use Binary Reader and Writer provided by NebulaModAPI class and save data into bytearray
        public byte[] data { get; set; }

        public MegastructurePacket() { } //Make sure to keep default constructor

        // If you need more examples check NebulaModel/Packets subfolder
        public MegastructurePacket(int planetId, int customValue)
        {
            this.planetId = planetId;
            this.customValue = customValue; // -1：请求全部巨构数据。 -2：回应，内含全部巨构数据。 -3：请求特定恒星的巨构数据。 >=0：携带的恒星的巨构类型

            // Using Binary Writer
            using IWriterProvider p = NebulaModAPI.GetBinaryWriter();

            if (customValue == -1 || customValue == -3) // 发送包，请求同步全部或是特定的巨构数据
            {

            }
            else if(customValue == -2) // 发送包，提供全部巨构数据
            {
                for (int i = 0; i < MoreMegaStructure.StarMegaStructureType.Length; i++)
                {
                    p.BinaryWriter.Write(MoreMegaStructure.StarMegaStructureType[i]);
                }
            }
            data = p.CloseAndGetBytes();
        }
    }

    [RegisterPacketProcessor] // This attribute lets Nebula know that this is the processor class for your new packet type
    public class YourCustomPacketProcessor : BasePacketProcessor<MegastructurePacket>
    {
        public override void ProcessPacket(MegastructurePacket packet, INebulaConnection conn)
        {
            //PlanetData planet = GameMain.galaxy.PlanetById(packet.planetId);
            // Handle received packets here. If you need more examples check NebulaNetwork/PacketProcessors subfolder

            if (packet.customValue == -1 && IsHost) // 回应全部恒星的巨构信息
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket<MegastructurePacket>(new MegastructurePacket(101, -2));
            }
            else if (packet.customValue == -2 && IsClient) // 加载主机回应的全部巨构信息
            {
                using IReaderProvider p = NebulaModAPI.GetBinaryReader(packet.data);

                for (int i = 0; i < MoreMegaStructure.StarMegaStructureType.Length; i++)
                {
                    MoreMegaStructure.StarMegaStructureType[i] = p.BinaryReader.ReadInt32();
                }
            }
            else if(packet.customValue == -3 && IsHost) // 回应所需的巨构种类
            {
                int starIndex = packet.planetId / 100 - 1;
                NebulaModAPI.MultiplayerSession.Network.SendPacket<MegastructurePacket>(new MegastructurePacket(packet.planetId, MoreMegaStructure.StarMegaStructureType[starIndex]));
            }
            else //同步对应恒星的巨构种类
            {
                int starIndex = packet.planetId / 100 - 1;
                MoreMegaStructure.StarMegaStructureType[starIndex] = packet.customValue;
                if(IsHost)
                {
                    DataSync.SetMegaType(starIndex, packet.customValue);
                }
            }
        }
    }


    public class DataSync
    {
        public static void SetMegaType(int starIndex, int type)
        {
            if (!NebulaModAPI.IsMultiplayerActive) return;

            NebulaModAPI.MultiplayerSession.Network.SendPacket<MegastructurePacket>(new MegastructurePacket((starIndex + 1) * 100 + 1, type));
        }

        public static void RequestAll()
        {
            if (!NebulaModAPI.IsMultiplayerActive) return;

            ILocalPlayer localPlayer = NebulaModAPI.MultiplayerSession.LocalPlayer;
            if(localPlayer.IsClient)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket<MegastructurePacket>(new MegastructurePacket(101, -1));
            }
        }

        public static void InitSendAllCountdown()
        {
            if (!NebulaModAPI.IsMultiplayerActive) return;

            MoreMegaStructure.broadcastAllDataCountdown = 60 * MoreMegaStructure.WaitSecToSyncDataWhenClientJoin.Value + 10;
        }

        public static void SendAll()
        {
            if (!NebulaModAPI.IsMultiplayerActive) return;

            ILocalPlayer localPlayer = NebulaModAPI.MultiplayerSession.LocalPlayer;
            if (localPlayer.IsHost)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket<MegastructurePacket>(new MegastructurePacket(101, -2));
            }
        }


        public static void RequestMegaType(int starIndex)
        {
            if (!NebulaModAPI.IsMultiplayerActive) return;

            ILocalPlayer localPlayer = NebulaModAPI.MultiplayerSession.LocalPlayer;
            if (localPlayer.IsClient)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket<MegastructurePacket>(new MegastructurePacket((starIndex + 1) * 100 + 1, -3));
            }
        }
    }
}
