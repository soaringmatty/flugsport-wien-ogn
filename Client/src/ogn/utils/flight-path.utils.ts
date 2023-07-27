import { Coordinate } from "ol/coordinate";

export function chaikinsAlgorithm(
  coords: Coordinate[],
  iterations: number = 3,
  factor: number = 0.25
): Coordinate[] {
  let result = coords;
  for (let i = 0; i < iterations; i++) {
    let smoothedCoords: Coordinate[] = [];
    for (let i = 0; i < result.length - 1; i++) {
      const p0 = result[i];
      const p1 = result[i + 1];

      const Q: Coordinate = [
        (1 - factor) * p0[0] + factor * p1[0],
        (1 - factor) * p0[1] + factor * p1[1],
      ];
      const R: Coordinate = [
        factor * p0[0] + (1 - factor) * p1[0],
        factor * p0[1] + (1 - factor) * p1[1],
      ];
      smoothedCoords.push(Q);
      smoothedCoords.push(R);
    }
    result = smoothedCoords;
  }
  // Add the original end points
  result.unshift(coords[0]);
  result.push(coords[coords.length - 1]);
  return result;
}
