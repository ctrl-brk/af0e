namespace AF0E.DB.Models;

public class PotaHunting
{
    public int Id { get; set; }
   public int LogId { get; set; }
    public int ParkId { get; set; }
    public bool P2P { get; set; }
    public string? BandReported { get; set; }

    public HrdLog Log { get; set; } = null!;

    public PotaPark Park { get; set; } = null!;
}
