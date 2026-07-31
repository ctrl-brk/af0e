// TypeScript needs an explicit module declaration for this Vite worker-query import.
declare module 'mapbox-gl/dist/mapbox-gl-csp-worker.js?worker' {
  const MapboxWorker: new () => Worker;
  export default MapboxWorker;
}



