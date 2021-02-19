// eslint-disable-next-line @typescript-eslint/no-unused-vars
import React, { FC, useContext, ChangeEvent } from 'react';
import { FormContext } from './Form';
/** @jsx jsx */
import { css, jsx } from '@emotion/core';
import { fontFamily, fontSize, gray5, gray2, gray6 } from './Styles';

interface Props {
  name: string;
  label?: string;
  // union type of string literals:
  type?: 'Text' | 'TextArea' | 'Password';
}

// common fild styles
const baseCSS = css`
  box-sizing: border-box;
  font-family: ${fontFamily};
  font-size: ${fontSize};
  margin-bottom: 5px;
  padding: 8px 10px;
  border: 1px solid ${gray5};
  border-radius: 3px;
  color: ${gray2};
  background-color: white;
  width: 100%;
  :focus {
    outline-color: ${gray5};
  }
  :disabled {
    background-color: ${gray6};
  }
`;

export const Field: FC<Props> = ({ name, label, type = 'Text' }) => {
  // get state setting function of context:
  const { setValue, touched, setTouched, validate } = useContext(FormContext);
  // handler for user typing in input and textarea components
  const handleChange = (
    e: ChangeEvent<HTMLInputElement> | ChangeEvent<HTMLTextAreaElement>,
  ) => {
    if (setValue) {
      setValue(name, e.currentTarget.value);
    }
    if (touched[name]) {
      if (validate) {
        validate(name);
      }
    }
  };

  const handleBlur = () => {
    if (setTouched) {
      setTouched(name);
    }
    if (validate) {
      validate(name);
    }
  };

  return (
    // at top level is context component of Form
    // this makes Fild connected to Form (something as observable or event)
    <FormContext.Consumer>
      {({ values, errors }) => (
        // styling container element
        <div
          css={css`
            display: flex;
            flex-direction: column;
            margin-bottom: 15px;
          `}
        >
          {/* <label htmlFor={name} >...</label> ties with */}
          {/* <input id={name} /> or <textarea id={name} /> */}
          {label && (
            <label
              css={css`
                font-weight: bold;
              `}
              htmlFor={name}
            >
              {label}
            </label>
          )}
          {/* text or password is input */}
          {(type === 'Text' || type === 'Password') && (
            // input value is bind with state (from context)  by name as key
            <input
              type={type.toLowerCase()}
              id={name}
              css={baseCSS}
              value={values[name] === undefined ? '' : values[name]}
              onChange={handleChange}
              onBlur={handleBlur}
            />
          )}
          {/* TextArea is textarea */}
          {type === 'TextArea' && (
            // textarea value is bind with state (from context) by name as key
            <textarea
              id={name}
              css={css`
                ${baseCSS};
                height: 100px;
              `}
              value={values[name] === undefined ? '' : values[name]}
              onChange={handleChange}
              onBlur={handleBlur}
            />
          )}
          {errors[name] &&
            errors[name].length > 0 &&
            errors[name].map((error) => (
              <div
                key={error}
                css={css`
                  font-size: 12px;
                  color: red;
                `}
              >
                {error}
              </div>
            ))}
        </div>
      )}
    </FormContext.Consumer>
  );
};
