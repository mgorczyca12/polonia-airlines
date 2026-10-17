using flight_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class SeatMapConfiguration : IEntityTypeConfiguration<SeatMap>
{
    public void Configure(EntityTypeBuilder<SeatMap> builder)
    {
        builder.ToTable("SeatMaps");
        builder.HasKey(seatMap => seatMap.Id);
        builder.HasMany(seatMap => seatMap.SeatMapRows).WithOne(row => row.SeatMap)
            .HasForeignKey(row => row.SeatMapId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(seatMap => seatMap.SeatMapRows)
            .HasField("seatMapRows").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
