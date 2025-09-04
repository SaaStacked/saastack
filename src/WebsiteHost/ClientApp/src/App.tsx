import { useEffect } from 'react';
import './main.css';

function App() {
  useEffect(() => {
    // TODO: main entry point for the page
  }, []);

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center">
      <div className="text-center">
        <h1 className="text-4xl font-bold text-gray-900 mb-4">Welcome to SaaStack</h1>
        <p className="text-xl text-gray-600 mb-8">Your React application is ready!</p>
        <div className="inline-flex items-center px-4 py-2 bg-primary-600 text-white rounded-lg shadow-md hover:bg-primary-700 transition-colors">
          <span>Get Started</span>
        </div>
      </div>
    </div>
  );
}

export default App;
