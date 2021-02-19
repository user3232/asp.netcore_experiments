/** @jsx jsx */
import { css, jsx } from '@emotion/core';
import { fontFamily, fontSize, gray1, gray2, gray5 } from './Styles';
import { UserIcon } from './Icons';
import { ChangeEvent, FC, useState, FormEvent } from 'react';
import { Link, RouteComponentProps, withRouter } from 'react-router-dom';

export const Header: FC<RouteComponentProps> = ({ history, location }) => {
  const searchParams = new URLSearchParams(location.search);
  const criteria = searchParams.get('criteria') || '';

  const [search, setSearch] = useState(criteria);

  const handleSearchInputChange = (e: ChangeEvent<HTMLInputElement>) => {
    setSearch(e.currentTarget.value);
    // console.log(e.currentTarget.value);
  };
  const handleSearchSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    history.push(`/search?criteria=${search}`);
  };
  return (
    // {/* flex box at top of container */}
    <div
      css={css`
        position: fixed;
        box-sizing: border-box;
        top: 0;
        width: 100%;
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 10px 20px;
        background-color: #fff;
        border-bottom: 1px solid ${gray5};
        box-shadow: 0 3px 7px 0 rgba(110, 112, 114, 0.21);
      `}
    >
      {/* navigate to main page */}
      <Link
        to="/"
        css={css`
          font-size: 24px;
          font-weight: bold;
          color: ${gray1};
          text-decoration: none;
        `}
      >
        Q & A
      </Link>
      {/* search for question input */}
      {/* form element is needed for sending on enter key hit */}
      <form onSubmit={handleSearchSubmit}>
        <input
          type="text"
          placeholder="Search..."
          value={search}
          onChange={handleSearchInputChange}
          css={css`
            box-sizing: border-box;
            font-family: ${fontFamily};
            font-size: ${fontSize};
            padding: 8px 10px;
            border: 1px solid ${gray5};
            border-radius: 3px;
            color: ${gray2};
            background-color: white;
            width: 200px;
            height: 30px;
            :focus {
              outline-color: ${gray5};
            }
          `}
        />
      </form>
      {/* login link */}
      <Link
        to="/signin"
        css={css`
          font-family: ${fontFamily};
          font-size: ${fontSize};
          padding: 5px 10px;
          background-color: transparent;
          color: ${gray2};
          text-decoration: none;
          cursor: pointer;
          span {
            margin-left: 10px;
          }
          :focus {
            outline-color: ${gray5};
          }
        `}
      >
        <UserIcon />
        <span>Sign In</span>
      </Link>
    </div>
  );
  // return <div>yoyo</div>;
  // is equivalent with:
  // return (<div>yoyo</div>);
  // but () allows:
  /* 
  return (
    <div>yoyo</div>
  );
  */
};

// below makes { history, location} available:
export const HeaderWithRouter = withRouter(Header);
