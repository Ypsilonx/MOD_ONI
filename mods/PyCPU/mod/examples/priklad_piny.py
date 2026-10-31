# Příklad: každý drát ribbonu na OUT0 bliká jinou rychlostí.
# Zapojení: OUT0 → ribbon → Ribbon Reader (4×) → 4 lampy.

# Délka půlperiody v ticích pro bit 0..3 (5 ticků = 1 s).
PERIODS = [1, 2, 5, 10]


def tick(io, storage):
    value = 0
    for bit, period in enumerate(PERIODS):
        if (io.tick // period) % 2:
            value |= 1 << bit
    io.out[0] = value
