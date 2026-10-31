# Příklad: čítač pulzů se storage (počet přežije uložení savu).
# IN0: signál k počítání (počítá se přechod 0 → 1), IN1: zelená = vynulovat.
# OUT0: spodní 4 bity počtu (ribbon), OUT1: zelená, když počet dosáhl LIMIT.

LIMIT = 10


def start(io, storage):
    storage.setdefault("count", 0)
    storage.setdefault("last", 0)


def tick(io, storage):
    signal = io.inp[0] & 1
    if io.inp[1] & 1:
        storage["count"] = 0
    elif signal and not storage["last"]:
        storage["count"] += 1
        io.log("pulz", storage["count"])
    storage["last"] = signal

    io.out[0] = storage["count"] & 0b1111
    io.out[1] = 1 if storage["count"] >= LIMIT else 0
