using ClassicUs.ManuAPI;

namespace ClassicUs.MedicMod
{
    internal class MedicRoleDescriptor : CustomCrewmateRole
    {
        public override string DisplayName => "Medic";
        public override string RoleTypeName => "MedicModRole";
        public override int Count => MedicAPIPlugin.ActiveEnabled ? MedicAPIPlugin.ActiveCount : 0;
        public override float RoleChancePercent => MedicAPIPlugin.ActiveRoleChance;
        public override string Description => "You are the Medic. Stand near a dead body and use your ability to revive them.";
        public override string DescriptionShort => "Revive dead bodies.";
        public override string EjectionText(string playerName) => $"{playerName} was the Medic.";
    }
}
