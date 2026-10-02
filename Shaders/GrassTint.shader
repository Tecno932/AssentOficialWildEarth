Orden recomendado
🪨 Ores y distribución de recursos
Copper
Iron
Gold
Después ampliar a otros minerales.
Veins, frecuencia, profundidad y distribución por bioma.
Verificar que no reemplacen bloques incorrectamente.
💧 Fluidos
Water.
Generación inicial.
Propagación horizontal/vertical.
Actualización entre chunks.
Persistencia.
Después Lava.

🌍 Generación del mundo

Revisar el pipeline completo:
Biome
   ↓
Terrain
   ↓
Caves
   ↓
Ores
   ↓
Fluids
   ↓
Mesh
Validar fronteras entre chunks.
Validar chunks cargados/descargados.
Validar generación determinista.
Validar mundo guardado/cargado.
🌳 Generación de estructuras
Árboles.
Vegetación.
Rocas.
Estructuras pequeñas.
Más adelante estructuras grandes.
⛏️ Interacción con bloques
Romper.
Colocar.
Actualizar mesh.
Actualizar vecinos.
Actualizar fluidos afectados.
📦 Sistema de recursos
Drops.
Items.
Inventario.
Herramientas.
Minería.
⚙️ Optimización final
Streaming.
LOD si realmente hace falta.
Greedy/Binary Greedy adicional.
Jobs/Burst.
Memoria.
Generación y meshing en paralelo.




antes de empezar tengo una duda, es posible reducir las texturas para aumentar los fps, ademas de que solo se vicibilicen las caras de los chunk que puedo ver, si nose ve mas abajo que no se renderice







pero aca tenemos un punto importante, quiero que las herramientas tengan mas de un sistema integrado, por ejemplo

Pico de hierro = Dureza del mango, dureza de la pua, filo

Espada de hierro = dureza en si, filo

Pala de hierro = Dureza del mango, dureza de la hoja, filo

Acha de hierro = Dureza del mango, dureza de la hoja, filo

todo eso dará un porcentaje del 100%, si la hoja tiene filo es mas rapida al picar su bloque y hace mas daño, la dureza de sus dos partes no afectan en nada, solo dice que parte se rompe primero, si se rompe la cabesa se da la cabeza con el valor de la cabesa de la erramienta completa mas un item de astillas, si se rompe la cabeza se rompe el item completo y se dan astillas mas un pico roto de su material correspondiente, si una erramienta, por ejemplo

Pico de cobre = toolspeed 1, el uno es la velocidad, el porcentaje de filo es el bonus que se le da, 100% de filo toolspeed 2, 50% toolspeed 1, 0% se reduce a 0,01, sin importar la herramienta