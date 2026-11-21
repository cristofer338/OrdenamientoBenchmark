# OrdenamientoBenchmark
# Unidad 5: Métodos de ordenamiento

Práctica de programación y banco de pruebas en **C# (.NET Aplicación de consola)** desarrollada en **Visual Studio** para comparar algoritmos de ordenamiento **interno** y **externo** en distintos tamaños de entrada y condiciones de ordenamiento.

## Competencia específica
Conoce, comprende y aplica los algoritmos de ordenamiento para el uso adecuado en el desarrollo de aplicaciones que permitan solucionar problemas del entorno.

---

## Algoritmos implementados

### 1. Ordenamiento interno (en memoria principal)
- **Burbuja (`BubbleSort`):** Compara pares adyacentes e intercambia si están desordenados; incorpora bandera de corte temprano si una pasada completa no realiza intercambios.
- **Quicksort (`Quicksort`):** Algoritmo de divide y vencerás mediante partición alrededor de un pivote, con recursión optimizada hacia la sublista más pequeña para acotar la pila de llamadas.
- **ShellSort (`ShellSort`):** Generalización del ordenamiento por inserción con intervalos decrecientes usando la secuencia de incrementos de Knuth.
- **Radix Sort (`RadixSort`):** Ordenamiento no comparativo por dígitos (base 10) de forma estable mediante conteo por posición.

### 2. Ordenamiento externo (apoyado en archivos temporales)
- **Intercalación (`ExternalIntercalacion`):** Divide los datos en bloques ordenados en archivos temporales y realiza fusiones balanceadas por parejas hasta consolidar el resultado.
- **Mezcla Directa (`ExternalMezclaDirecta`):** Genera tramos/corridas iniciales ordenadas de tamaño fijo y los combina en pasadas sucesivas de fusión de dos archivos.
- **Mezcla Natural (`ExternalMezclaNatural`):** Identifica corridas ya ordenadas naturalmente en la secuencia de entrada para reducir pasadas de fusión cuando existe orden previo.

---

## Diseño del banco de pruebas

El programa genera arreglos de números enteros (`int[]`) bajo una semilla fija (`seed = 42`) para garantizar repetibilidad en las comparaciones:

- **Tamaños evaluados:** `10000`, `50000` y `100000` registros.
- **Escenarios / condiciones:**
  - `aleatorio`: valores distribuidos de forma pseudoaleatoria.
  - `ordenado`: datos previamente ordenados de forma ascendente.
  - `inverso`: datos ordenados de forma descendente.
- **Medición de rendimiento:** Se mide el **tiempo de CPU** del proceso (`Process.GetCurrentProcess().TotalProcessorTime`) en segundos y se verifica automáticamente que la salida quede ordenada (`ok=True`).
- **Control de `Burbuja`:** En tamaños mayores a `10000` registros se omite (`[SKIP]`) por su complejidad cuadrática $O(n^2)$ para evitar tiempos de ejecución excesivos.

Al concluir, se exporta **`resultados_benchmark.csv`** con el formato:
`algoritmo,tipo,tamano,escenario,tiempo_cpu_seg,valido`

---

## Resumen de complejidades teóricas

| Algoritmo | Tipo | Mejor caso | Caso promedio | Peor caso | Espacio auxiliar | Estable |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Burbuja** | Interno | $O(n)$ | $O(n^2)$ | $O(n^2)$ | $O(1)$ | Sí |
| **Quicksort** | Interno | $O(n \log n)$ | $O(n \log n)$ | $O(n^2)$ | $O(\log n)$ | No |
| **ShellSort** | Interno | $O(n \log n)$ | $O(n^{1.25})$–$O(n^{1.5})$ | $O(n^2)$ | $O(1)$ | No |
| **Radix Sort** | Interno | $O(k(n + d))$ | $O(k(n + d))$ | $O(k(n + d))$ | $O(n + d)$ | Sí |
| **Intercalación** | Externo | $O(N \log_2 N)$ | $O(N \log_2 N)$ | $O(N \log_2 N)$ | $O(B)$ | Sí |
| **Mezcla Directa** | Externo | $O(N \log_2 N)$ | $O(N \log_2 N)$ | $O(N \log_2 N)$ | $O(B)$ | Sí |
| **Mezcla Natural** | Externo | $O(N)$ | $O(N \log_2 N)$ | $O(N \log_2 N)$ | $O(B)$ | Sí |

---

## Observaciones clave del experimento
- Los **algoritmos internos** (`Quicksort`, `ShellSort` y `Radix Sort`) presentan tiempos de CPU muy bajos al operar en RAM sin escrituras/lecturas constantes en disco.
- En **Mezcla Natural**, cuando la entrada ya está ordenada, se forma una sola corrida y el esfuerzo de fusión es mínimo; en cambio, cuando los datos vienen en orden inverso o aleatorio, se fragmentan en muchas corridas pequeñas y el costo de lectura/escritura y fusión de archivos sube notablemente.
- Los **algoritmos externos** son necesarios cuando el volumen de datos supera la memoria disponible y el acceso secuencial a archivos forma parte del flujo del sistema.

---

## Estructura del repositorio

```text
unidad5-metodos-ordenamiento/
├── OrdenamientoBenchmark/
│   ├── Program.cs
│   └── OrdenamientoBenchmark.csproj
├── .gitignore
├── README.md
└── reporte/                  # Tablas de rendimiento, gráficas y reporte de práctica
