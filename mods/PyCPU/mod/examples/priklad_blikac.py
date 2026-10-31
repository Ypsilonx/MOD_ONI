# Příklad: blikač. OUT0 se každou sekundu přepne mezi 0 a 1.
# Zapojení: OUT0 → lampa (nebo cokoli s automatizačním vstupem).


def tick(io, storage):
    # 5 ticků = 1 s; (io.tick // 5) se zvětší o 1 každou sekundu, % 2 střídá 0 a 1.
    io.out[0] = (io.tick // 5) % 2
