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