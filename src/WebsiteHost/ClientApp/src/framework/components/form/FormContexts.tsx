import React from 'react';
import { ValidationMode } from 'react-hook-form';
import { ActionResult } from '../../actions/Actions.ts';


export const ActionFormRequiredFieldsContext = React.createContext<string[]>([]);
export const ActionFormContext = React.createContext<ActionResult<any, any, any> | undefined>(undefined);
export const ActionFormValidationContext = React.createContext<keyof ValidationMode>('onBlur');
