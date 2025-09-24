import React, { useState } from 'react';

interface TabsProps {
  tabs: Tab[];
  defaultTab?: string;
}

interface Tab {
  id: string;
  label: string;
  content: React.ReactNode;
}

// Create a set of tabs, that can be used to switch between different views.
export const FormTabs: React.FC<TabsProps> = ({ tabs, defaultTab }) => {
  const [activeTab, setActiveTab] = useState(defaultTab || tabs[0]?.id);

  return (
    <div className="w-full">
      <div className="border-b border-gray-200 dark:border-gray-600">
        <nav className="-mb-px flex space-x-8">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`py-2 px-1 border-b-2 font-medium text-sm transition-colors ${
                activeTab === tab.id
                  ? 'border-primary text-primary dark:text-primary-light'
                  : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-500'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      <div className="mt-6">{tabs.find((tab) => tab.id === activeTab)?.content}</div>
    </div>
  );
};
