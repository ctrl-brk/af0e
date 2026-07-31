// TypeScript needs an explicit module declaration for this Vite query-suffixed URL import.
declare module 'mapbox-gl/dist/mapbox-gl-csp-worker.js?url' {
  const url: string;
  export default url;
}



