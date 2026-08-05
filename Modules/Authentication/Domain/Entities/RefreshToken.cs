using System;

namespace Modules.Authentication.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public Guid FamilyId { get; set; } = Guid.NewGuid();
        public DateTime? RevokedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public static RefreshToken CreateNew(Guid userId, string tokenHash, TimeSpan lifetime)
        {
            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.Add(lifetime),
                FamilyId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
        }

        public static RefreshToken CreateChild(RefreshToken parent, string newHash, TimeSpan lifetime)
        {
            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = parent.UserId,
                TokenHash = newHash,
                ExpiresAt = DateTime.UtcNow.Add(lifetime),
                FamilyId = parent.FamilyId,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
